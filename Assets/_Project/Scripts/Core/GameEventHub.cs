using System;
using UnityEngine;

namespace LiteRealm.Core
{
    [Serializable]
    public struct WeaponFiredEvent
    {
        public Vector3 Position;
        public float Loudness;
        public GameObject Instigator;
    }

    [Serializable]
    public struct DamageDealtEvent
    {
        public string SourceId;
        public float Amount;
        public Vector3 Position;
        public Vector3 Normal;
        public GameObject Target;
        public GameObject Instigator;
        public bool Killed;
        public bool IsCritical;
    }

    [Serializable]
    public struct EnemyKilledEvent
    {
        public string EnemyId;
        public GameObject EnemyObject;
        public GameObject Killer;
    }

    [Serializable]
    public struct BossKilledEvent
    {
        public string BossId;
        public GameObject BossObject;
        public GameObject Killer;
    }

    [Serializable]
    public struct ItemCollectedEvent
    {
        public string ItemId;
        public int Amount;
        public GameObject Collector;
    }

    [Serializable]
    public struct ObjectiveSignalEvent
    {
        public string ObjectiveId;
        public int Amount;
        public GameObject Sender;
        public GameObject Instigator;
    }

    public class GameEventHub : MonoBehaviour
    {
        public event Action<WeaponFiredEvent> WeaponFired;
        public event Action<DamageDealtEvent> DamageDealt;
        public event Action<EnemyKilledEvent> EnemyKilled;
        public event Action<BossKilledEvent> BossKilled;
        public event Action<ItemCollectedEvent> ItemCollected;
        public event Action<ObjectiveSignalEvent> ObjectiveSignaled;
        public event Action<bool> NightStateChanged;
        public event Action<float, int> TimeChanged;

        public void RaiseWeaponFired(WeaponFiredEvent data)
        {
            WeaponFired?.Invoke(data);
        }

        public void RaiseDamageDealt(DamageDealtEvent data)
        {
            DamageDealt?.Invoke(data);
        }

        public void RaiseEnemyKilled(EnemyKilledEvent data)
        {
            EnemyKilled?.Invoke(data);
        }

        public void RaiseBossKilled(BossKilledEvent data)
        {
            BossKilled?.Invoke(data);
        }

        public void RaiseItemCollected(ItemCollectedEvent data)
        {
            ItemCollected?.Invoke(data);
        }

        public void RaiseObjectiveSignaled(ObjectiveSignalEvent data)
        {
            ObjectiveSignaled?.Invoke(data);
        }

        public void RaiseNightStateChanged(bool isNight)
        {
            NightStateChanged?.Invoke(isNight);
        }

        public void RaiseTimeChanged(float normalizedTime, int dayNumber)
        {
            TimeChanged?.Invoke(normalizedTime, dayNumber);
        }
    }
}
