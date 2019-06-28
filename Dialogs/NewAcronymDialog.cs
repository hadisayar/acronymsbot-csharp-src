using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EchoBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class NewAcronymDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<AcronymModel> _acronymDefinitionAccessor;
        public NewAcronymDialog(UserState userState) : base(nameof(NewAcronymDialog))
        {
            _acronymDefinitionAccessor = userState.CreateProperty<AcronymModel>("NewAcronym");
            var waterfallSteps = new WaterfallStep[]
            {
                ChoiceStepAsync,
                AcronymNameStepAsync,
                AcronymNameConfirmStepAsync,
                FullNameStepAsync,
                FullNameConfirmStepAsync,
                DefinitionStepAsync,
                DefinitionConfirmStepAsync,
                //EGStepAsync,
                //EGConfirmStepAsync,
                //OrgStepAsync,
                //OrgConfirmStepAsync,
                //TeamStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
        }


        private static async Task<DialogTurnResult> ChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What would you like to do?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Add a new acronym", "Search Acronyms (TBD)", "Leave a comment (TBD)" }),
                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> AcronymNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["ChoiceMade"] = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the acronym you wish to add.") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> AcronymNameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[nameof(AcronymModel.Acronym)] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Is this the acronym you wish to add? {stepContext.Result}.") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> FullNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"What does {stepContext.Values[nameof(AcronymModel.Acronym)]} stand for?") }, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync();
            }
        }

        private static async Task<DialogTurnResult> FullNameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[nameof(AcronymModel.Fullname)] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Does {stepContext.Values[nameof(AcronymModel.Acronym)]} really stand for {stepContext.Result}?") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> DefinitionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"What does {stepContext.Values[nameof(AcronymModel.Acronym)]} mean?") }, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync();
            }
        }

        private static async Task<DialogTurnResult> DefinitionConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[nameof(AcronymModel.Definition)] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Does {stepContext.Values[nameof(AcronymModel.Acronym)]} really stand for {stepContext.Result}?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var msg = $@"Input: 
    Acronym: {(string)stepContext.Values[nameof(AcronymModel.Acronym)]}
    FullName: {(string)stepContext.Values[nameof(AcronymModel.Fullname)]}
    Definition: {(string)stepContext.Values[nameof(AcronymModel.Definition)]}";

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is a Prompt Dialog.
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Is this ok?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                // Get the current profile object from user state.
                var newAcronym = await _acronymDefinitionAccessor.GetAsync(stepContext.Context, () => new AcronymModel(), cancellationToken);

                newAcronym.Acronym = (string)stepContext.Values[nameof(AcronymModel.Acronym)];
                newAcronym.Fullname = (string)stepContext.Values[nameof(AcronymModel.Fullname)];
                newAcronym.Definition = (string)stepContext.Values[nameof(AcronymModel.Definition)];

                var msg = $@"Your final input: 
    Acronym: {newAcronym.Acronym}
    FullName: {newAcronym.Fullname}
    Definition: {newAcronym.Definition}";

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks. Your profile will not be kept."), cancellationToken);
            }

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}
