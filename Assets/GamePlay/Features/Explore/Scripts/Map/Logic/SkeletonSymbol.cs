using Core.Scripts.Foundation.Define;
using System;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    public class SkeletonSymbol
    {
        public readonly int Id;                  
        public readonly SystemEnum.MapSymbolType Type;       
        public readonly int X;
        public readonly int Y;
        public readonly SystemEnum.CellEventType? EventType;
        public readonly long? ItemIndex;          
        public readonly bool IsDeleted;           

        private SkeletonSymbol(
            int id, 
            SystemEnum.MapSymbolType type, 
            int x, int y,
            SystemEnum.CellEventType? eventType, 
            long? itemIndex, 
            bool deleted)
        {
            Id = id;
            Type = type;
            X = x;
            Y = y;
            EventType = eventType;
            ItemIndex = itemIndex;
            IsDeleted = deleted;
        }


        public static SkeletonSymbol CreateSimple(int id, int x, int y, SystemEnum.MapSymbolType type)
        {
            if (type == SystemEnum.MapSymbolType.Event || type == SystemEnum.MapSymbolType.Item)
                throw new ArgumentException("Use CreateEvent/CreateItem for typed payload");
            return new SkeletonSymbol(id, type, x, y, null, null, deleted: false);
        }

        public static SkeletonSymbol CreateEvent(int id, int x, int y, SystemEnum.CellEventType eventType)
        {
            return new SkeletonSymbol(id, SystemEnum.MapSymbolType.Event, x, y, eventType, null, deleted: false);
        }

        public static SkeletonSymbol CreateItem(int id, int x, int y, long itemIndex)
        {
            return new SkeletonSymbol(id, SystemEnum.MapSymbolType.Item, x, y, null, itemIndex, deleted: false);
        }

        public static SkeletonSymbol Deleted(int id) =>
            new SkeletonSymbol(id, SystemEnum.MapSymbolType.None, -1, -1, null, null, deleted: true);
    }
}