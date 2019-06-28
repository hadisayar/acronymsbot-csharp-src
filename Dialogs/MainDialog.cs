using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EchoBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class MainDialog : ComponentDialog
    {

        private readonly UserState _userState;

        public MainDialog(UserState userState)
            : base(nameof(MainDialog))
        {
            _userState = userState;

            AddDialog(new NewAcronymDialog(userState));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(NewAcronymDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var acronymModel = (AcronymModel)stepContext.Result;

            string status = "You are signed up to review "
                + (acronymModel.Acronym.Length is 0 ? "no companies" : string.Join(": ", acronymModel.Definition))
                + ".";

            await stepContext.Context.SendActivityAsync(status);

            var accessor = _userState.CreateProperty<AcronymModel>(nameof(AcronymModel));
            await accessor.SetAsync(stepContext.Context, acronymModel, cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
