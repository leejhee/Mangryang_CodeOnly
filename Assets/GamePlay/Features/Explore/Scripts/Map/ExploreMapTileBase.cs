using System.Text;
using UnityEngine;

namespace GamePlay.Explore.Map
{
    /// <summary>
    /// 맵에 깔릴 타일 오브젝트의 베이스. 
    /// </summary>
    public abstract class ExploreMapTileBase : MonoBehaviour
    {
        [SerializeField] private long id;
        [SerializeField] private Vector2Int cellPos;
        [SerializeField] private SpriteRenderer tileSprite;
        
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append("{").Append($"x : {cellPos.x} | y : {cellPos.y}").Append("}");
            return sb.ToString();
        }
    }
}
