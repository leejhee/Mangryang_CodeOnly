using System;

public interface IState<T> where T : class
{
    public void Enter(T entity); //들어갈 때
    public void Execute(T entity); //해당 상태에서의 실행
    public void Exit(T entity); //나갈때

}