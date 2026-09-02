using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;

    private readonly HashSet<string> collectedObjectIds = new();
    private GameSaveData pendingData;

    public static SaveManager Instance { get; private set; }
    public bool HasSave => File.Exists(SavePath);

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "mine-save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void MarkObjectCollected(string objectId)
    {
        if (!string.IsNullOrWhiteSpace(objectId))
            collectedObjectIds.Add(objectId);
    }

    public bool IsObjectCollected(string objectId)
    {
        return !string.IsNullOrWhiteSpace(objectId) &&
               collectedObjectIds.Contains(objectId);
    }

    public void SaveCheckpoint()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        HotbarController hotbar = FindFirstObjectByType<HotbarController>();

        if (inventory == null || health == null || hotbar == null)
        {
            Debug.LogError(
                "SaveManager: не найдены PlayerInventory, PlayerHealth или HotbarController.",
                this
            );
            return;
        }

        GameSaveData data = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = new Vector3SaveData(inventory.transform.position),
            playerYaw = inventory.transform.eulerAngles.y,
            playerHealth = health.CurrentHealth,
            selectedHotbarIndex = hotbar.SelectedIndex
        };

        foreach (InventorySlotData slot in inventory.Slots)
        {
            data.inventory.Add(new InventorySlotSaveData
            {
                itemId = slot != null && !slot.IsEmpty
                    ? slot.Item.ItemId
                    : string.Empty,
                amount = slot != null && !slot.IsEmpty
                    ? slot.Amount
                    : 0
            });
        }

        data.hotbarItemIds = hotbar.GetAssignmentItemIds();

        data.collectedObjectIds.AddRange(collectedObjectIds);
        SaveTorchStates(data);
        SaveDoorStates(data);
        SaveTorchSocketStates(data);
        SaveChestStates(data);

        MonsterDirector director =
            FindFirstObjectByType<MonsterDirector>();
        data.monsterReleased = director != null && director.IsReleased;

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"Контрольная точка сохранена: {SavePath}");
    }

    public void LoadCheckpoint()
    {
        if (!HasSave)
        {
            Debug.LogWarning("SaveManager: файл сохранения отсутствует.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        pendingData = JsonUtility.FromJson<GameSaveData>(json);

        if (pendingData == null || string.IsNullOrWhiteSpace(pendingData.sceneName))
        {
            Debug.LogError("SaveManager: файл сохранения повреждён.");
            pendingData = null;
            return;
        }

        SceneManager.LoadScene(pendingData.sceneName);
    }

    public void RestartFromBeginning()
    {
        DeleteSave();
        collectedObjectIds.Clear();
        pendingData = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingData != null)
            StartCoroutine(ApplyPendingDataNextFrame());
    }

    private IEnumerator ApplyPendingDataNextFrame()
    {
        yield return null;

        GameSaveData data = pendingData;
        pendingData = null;

        collectedObjectIds.Clear();

        if (data.collectedObjectIds != null)
        {
            foreach (string objectId in data.collectedObjectIds)
                MarkObjectCollected(objectId);
        }

        RemoveCollectedPickups();

        PlayerInventory inventory =
            FindFirstObjectByType<PlayerInventory>();
        PlayerHealth health =
            FindFirstObjectByType<PlayerHealth>();
        HotbarController hotbar =
            FindFirstObjectByType<HotbarController>();

        if (inventory != null)
        {
            CharacterController controller =
                inventory.GetComponent<CharacterController>();

            if (controller != null)
                controller.enabled = false;

            inventory.transform.position = data.playerPosition.ToVector3();
            inventory.transform.rotation =
                Quaternion.Euler(0.0f, data.playerYaw, 0.0f);

            if (controller != null)
                controller.enabled = true;

            inventory.RestoreFromSave(data.inventory, itemDatabase);
        }

        if (health != null)
            health.RestoreHealth(data.playerHealth);

        if (hotbar != null)
        {
            hotbar.RestoreAssignments(data.hotbarItemIds, itemDatabase);
            hotbar.SelectSlot(data.selectedHotbarIndex);
        }

        RestoreTorchStates(data);
        RestoreDoorStates(data);
        RestoreTorchSocketStates(data);
        RestoreChestStates(data);

        MonsterDirector director =
            FindFirstObjectByType<MonsterDirector>();

        if (director != null)
            director.RestoreReleasedState(data.monsterReleased);
    }

    private void RemoveCollectedPickups()
    {
        PersistentObjectId[] objects = FindObjectsByType<PersistentObjectId>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (PersistentObjectId persistentObject in objects)
        {
            if (!IsObjectCollected(persistentObject.Id))
                continue;

            if (persistentObject.GetComponent<InventoryPickup>() != null)
                Destroy(persistentObject.gameObject);
        }
    }

    private static void SaveTorchStates(GameSaveData data)
    {
        WorldTorch[] torches = FindObjectsByType<WorldTorch>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (WorldTorch torch in torches)
        {
            PersistentObjectId id = torch.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            data.torches.Add(new TorchSaveData
            {
                id = id.Id,
                isLit = torch.IsLit
            });
        }
    }

    private static void RestoreTorchStates(GameSaveData data)
    {
        if (data.torches == null)
            return;

        WorldTorch[] torches = FindObjectsByType<WorldTorch>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (WorldTorch torch in torches)
        {
            PersistentObjectId id = torch.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            foreach (TorchSaveData saved in data.torches)
            {
                if (saved.id == id.Id)
                {
                    torch.RestoreLitState(saved.isLit);
                    break;
                }
            }
        }
    }

    private static void SaveDoorStates(GameSaveData data)
    {
        InteractiveDoor[] doors = FindObjectsByType<InteractiveDoor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (InteractiveDoor door in doors)
        {
            PersistentObjectId id = door.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            data.doors.Add(new DoorSaveData
            {
                id = id.Id,
                isOpen = door.IsOpen,
                isLocked = door.IsLocked
            });
        }
    }

    private static void RestoreDoorStates(GameSaveData data)
    {
        if (data.doors == null)
            return;

        InteractiveDoor[] doors = FindObjectsByType<InteractiveDoor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (InteractiveDoor door in doors)
        {
            PersistentObjectId id = door.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            foreach (DoorSaveData saved in data.doors)
            {
                if (saved.id == id.Id)
                {
                    door.RestoreState(saved.isOpen, saved.isLocked);
                    break;
                }
            }
        }
    }

    private static void SaveTorchSocketStates(GameSaveData data)
    {
        TorchSocket[] sockets = FindObjectsByType<TorchSocket>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TorchSocket socket in sockets)
        {
            PersistentObjectId id = socket.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            data.torchSockets.Add(new TorchSocketSaveData
            {
                id = id.Id,
                isOccupied = socket.IsOccupied
            });
        }
    }

    private static void RestoreTorchSocketStates(GameSaveData data)
    {
        if (data.torchSockets == null)
            return;

        TorchSocket[] sockets = FindObjectsByType<TorchSocket>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TorchSocket socket in sockets)
        {
            PersistentObjectId id = socket.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            foreach (TorchSocketSaveData saved in data.torchSockets)
            {
                if (saved.id == id.Id)
                {
                    socket.RestoreState(saved.isOccupied);
                    break;
                }
            }
        }
    }

    private static void SaveChestStates(GameSaveData data)
    {
        LootChest[] chests = FindObjectsByType<LootChest>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (LootChest chest in chests)
        {
            PersistentObjectId id = chest.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            ChestSaveData savedChest = new ChestSaveData
            {
                id = id.Id,
                isOpen = chest.IsOpen,
                isLocked = chest.IsLocked
            };

            foreach (ChestLootEntry entry in chest.Contents)
            {
                savedChest.contents.Add(new InventorySlotSaveData
                {
                    itemId = entry != null && !entry.IsEmpty
                        ? entry.item.ItemId
                        : string.Empty,
                    amount = entry != null && !entry.IsEmpty
                        ? entry.amount
                        : 0
                });
            }

            data.chests.Add(savedChest);
        }
    }

    private void RestoreChestStates(GameSaveData data)
    {
        if (data.chests == null)
            return;

        LootChest[] chests = FindObjectsByType<LootChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (LootChest chest in chests)
        {
            PersistentObjectId id = chest.GetComponent<PersistentObjectId>();

            if (id == null)
                continue;

            foreach (ChestSaveData saved in data.chests)
            {
                if (saved.id == id.Id)
                {
                    chest.RestoreState(
                        saved.isOpen,
                        saved.isLocked,
                        saved.contents,
                        itemDatabase
                    );
                    break;
                }
            }
        }
    }
}