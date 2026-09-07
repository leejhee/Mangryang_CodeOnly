using UnityEngine;

namespace Core.Attributes
{
    public class CustomDisableAttribute : PropertyAttribute
    {
        public bool IsInitialized { get; set; }
    }
}
