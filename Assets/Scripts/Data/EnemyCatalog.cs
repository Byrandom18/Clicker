using System;
using UnityEngine;

namespace Clicker
{
    [Serializable]
    public class EnemyDef
    {
        public string id;
        public string nameRu;
        public string nameEn;
        public Color placeholderColor = Color.white;
        [Tooltip("4 sprites, one per health stage. Leave empty to use generated placeholders.")]
        public Sprite[] stageSprites;

        public string DisplayName => Loc.T(nameRu, nameEn);

        public Sprite GetSprite(int stageIndex)
        {
            if (stageSprites == null || stageSprites.Length == 0)
                return null;
            int i = Mathf.Clamp(stageIndex, 0, stageSprites.Length - 1);
            return stageSprites[i];
        }
    }

    [CreateAssetMenu(menuName = "Clicker/Enemy Catalog", fileName = "EnemyCatalog")]
    public class EnemyCatalog : ScriptableObject
    {
        public Sprite background;
        public EnemyDef[] enemies;

        public int Count => enemies != null ? enemies.Length : 0;

        public EnemyDef Get(int index)
        {
            if (enemies == null || index < 0 || index >= enemies.Length)
                return null;
            return enemies[index];
        }
    }
}
