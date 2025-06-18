using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace TeamsAssistBot.Core.Interfaces;

public interface IBotService
{
    Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken);
    Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken);
    Task OnTeamsChannelCreatedAsync(ChannelInfo channelInfo, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken);
    Task OnTeamsMeetingStartAsync(MeetingStartEventDetails meetingStartEventDetails, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken);
    Task OnTeamsMeetingEndAsync(MeetingEndEventDetails meetingEndEventDetails, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken);
    Task SendProactiveMessageAsync(string conversationId, string message);
    Task SendAdaptiveCardAsync(string conversationId, object cardData);
}
