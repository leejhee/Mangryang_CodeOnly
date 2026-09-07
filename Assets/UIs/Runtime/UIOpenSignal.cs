namespace Core.Scripts.UIAbstraction
{
    public readonly struct UIOpenSignal
    {
        public readonly string Route;
        public readonly object Payload;
        public UIOpenSignal(string route, object payload)
        {Route = route; this.Payload = payload;}
    }
}