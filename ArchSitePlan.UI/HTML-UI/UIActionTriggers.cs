using ArchSitePlan.UI.HTML_UI.MessagesFromUI;
using ArchSitePlan.UI.HTML_UI.MessageToUI;

namespace ArchSitePlan.UI.HTML_UI;

public class UIActionTriggers
{
    public static void InterpretMessage(IMessageFromUI<IMessageToUI> message)
    {
        switch (message.MessageType)
        {
            case TypeOfMessage.Transit:
                (message as TransitMessage)!.HandleMessage();
                break;

            case TypeOfMessage.Landuse:
                (message as LanduseMessage)!.HandleMessage();
                break;

            case TypeOfMessage.Building:
                (message as BuildingMessage)!.HandleMessage();
                break;

            case TypeOfMessage.LanduseListEdit:
                (message as LanduseListEditMessage)!.HandleMessage();
                break;

            case TypeOfMessage.Divider:
                (message as DividerMessage)!.HandleMessage();
                break;

            default:
                throw new NotImplementedException();
        }
    }

}
