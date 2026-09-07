using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Scripts.Data
{
    public abstract class SheetData
    {
        protected int ID = 0;

        public virtual UniTask<Dictionary<long, SheetData>> ParseAsync(string csv, CancellationToken ct) =>
            throw new NotImplementedException($"{GetType().Name} must override ParseAsync");
    }
    

}