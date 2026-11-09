using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stage_Wave_Info")]
public class Stage_Wave_Info : ScriptableObject
{
    [Header("스테이지 번호/최대 웨이브 수")]
    public int stageNumber;
    public int maxWaveNumber;
    public string stageName;

    [Header("몬스터 생성주기를 관리하는 리스트")]
    public List<MonsterSpawnData> spawnDataList = new List<MonsterSpawnData>();

    [System.Serializable]
    public class MonsterSpawnData
    {
        public enum MonsterType //몬스터 타입
        {
            None, Default, Boss
        }
        public int currentWaveNumber; //현재 웨이브 번호
        public float triggerTime; //해당 정보가 적용될 시간
        public Monster_Info monsterInfo; //생성될 몬스터 정보
        public MonsterType monsterType; //몬스터 타입
        public int MonsterCount; //몬스터 생성할 개수
    }
}
