using UnityEngine;

namespace Clicker
{
    [CreateAssetMenu(menuName = "Clicker/Enemy", fileName = "Enemy")]
    public class EnemyDef : ScriptableObject
    {
        public string id;
        public string nameRu;
        public string nameEn;
        public Color placeholderColor = Color.white;
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
}
