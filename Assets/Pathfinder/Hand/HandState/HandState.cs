public abstract class HandState
{
    public abstract void OnBegin(HandStateContext context);
    public abstract void OnUpdate(HandStateContext context);
    public abstract void OnPress(HandStateContext context);
    public abstract void OnRelease(HandStateContext context);
}