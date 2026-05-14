namespace AIQ.Urban.Core.ModelActions;

public interface IUrbanModelStep
{
    public List<Guid> Run(UrbanModel model);
}

public interface IAddUrbanElementsStep:IUrbanModelStep;
public interface IDeleteUrbanElementsStep:IUrbanModelStep;
public interface IEditUrbanElementsStep:IUrbanModelStep;
