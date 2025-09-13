namespace ArchSitePlan.Core.Models;
public class CoreModelSingelton
{
    public static CoreModel Instance { get; set; } = new CoreModel();
}
