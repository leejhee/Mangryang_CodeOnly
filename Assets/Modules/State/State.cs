using System;

public class State<T> : IState<T> where T : class
{
    public Action<T> enter;
    public Action<T> execute;
    public Action<T> exit;
    public virtual void Enter(T entity) => enter.Invoke(entity);   //들어갈 때
    public virtual void Execute(T entity) => execute.Invoke(entity); //해당 상태에서의 실행
    public virtual void Exit(T entity) => exit.Invoke(entity);    //나갈때
    public virtual string StateName => GetType().Name;
}