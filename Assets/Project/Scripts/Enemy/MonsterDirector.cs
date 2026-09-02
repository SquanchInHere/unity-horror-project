using UnityEngine;

public class MonsterDirector : MonoBehaviour
{
    [SerializeField] private MonsterAI monster;
    [SerializeField] private Transform player;
    [SerializeField] private bool startsReleased;

    public bool IsReleased { get; private set; }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        RestoreReleasedState(startsReleased);
    }

    public void ReleaseMonster()
    {
        if (IsReleased || monster == null || player == null)
            return;

        IsReleased = true;
        monster.Activate(player);
    }

    public void RestoreReleasedState(bool released)
    {
        IsReleased = false;

        if (monster == null)
            return;

        monster.SetDormant();

        if (released)
            ReleaseMonster();
    }
}
