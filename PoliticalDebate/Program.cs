using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

namespace PoliticalDebate;

public static class Program
{
    private static async Task Main(string[] args)
    {
        string theme = args.Length > 0
            ? string.Join(" ", args)
            : PromptForTheme();

        Console.WriteLine();
        Console.WriteLine("================================================================");
        Console.WriteLine($"  DEBAT POLITIQUE : {theme.ToUpperInvariant()}");
        Console.WriteLine("================================================================");
        Console.WriteLine();

        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Erreur : La variable d'environnement OPENAI_API_KEY n'est pas definie.");
            Console.WriteLine("Definissez-la avec : export OPENAI_API_KEY=sk-...");
            Console.ResetColor();
            return;
        }

        string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

        // Create the IChatClient from OpenAI
        IChatClient chatClient = new OpenAIClient(apiKey)
            .GetChatClient(model)
            .AsIChatClient();

        // --- Define the 3 agents ---

        ChatClientAgent interviewer = new(
            chatClient,
            """
            Tu es un journaliste politique chevronne qui anime un debat televise.
            Tu poses des questions incisives et relances les candidats.
            Tu veilles a ce que chaque camp s'exprime equitablement.
            Tu commences par presenter le theme du debat, puis tu poses une question a chaque candidat.
            A chaque tour, tu reformules ou approfondis la question en fonction des reponses precedentes.
            Tu restes neutre et factuel. Tu parles en francais.
            Tu t'adresses aux candidats par leur nom (Candidat de Gauche, Candidat de Droite).
            """,
            "Journaliste",
            "Un journaliste politique qui anime le debat");

        ChatClientAgent leftPolitician = new(
            chatClient,
            """
            Tu es un politicien francais de gauche, progressiste et engage.
            Tu defends la justice sociale, la redistribution des richesses, les services publics,
            l'ecologie, les droits des travailleurs et la solidarite.
            Tu argumentes avec passion mais reste respectueux de ton adversaire.
            Tu reponds aux questions du journaliste et tu reagis aux arguments de ton adversaire de droite.
            Tu parles en francais. Tu es concis (3-4 phrases maximum par intervention).
            """,
            "Candidat_de_Gauche",
            "Un politicien de gauche qui defend ses idees progressistes");

        ChatClientAgent rightPolitician = new(
            chatClient,
            """
            Tu es un politicien francais de droite, liberal et conservateur.
            Tu defends la liberte d'entreprise, la responsabilite individuelle, la securite,
            la reduction des impots, la maitrise de la depense publique et l'autorite de l'Etat.
            Tu argumentes avec conviction mais reste respectueux de ton adversaire.
            Tu reponds aux questions du journaliste et tu reagis aux arguments de ton adversaire de gauche.
            Tu parles en francais. Tu es concis (3-4 phrases maximum par intervention).
            """,
            "Candidat_de_Droite",
            "Un politicien de droite qui defend ses idees conservatrices");

        // --- Build group chat workflow: 3 rounds x 3 agents = 9 turns ---

        DebateGroupChatManager manager = new([interviewer, leftPolitician, rightPolitician])
        {
            MaximumIterationCount = 9
        };

        Workflow workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(_ => manager)
            .AddParticipants([interviewer, leftPolitician, rightPolitician])
            .Build();

        // --- Run the debate ---

        Console.WriteLine("  Le debat commence...");
        Console.WriteLine();

        List<ChatMessage> messages =
        [
            new(ChatRole.User, $"Le theme du debat d'aujourd'hui est : \"{theme}\". Presentez le sujet et lancez le debat.")
        ];

        await using StreamingRun run = await InProcessExecution.Lockstep.RunStreamingAsync(workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? lastExecutorId = null;
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent e:
                {
                    if (e.ExecutorId != lastExecutorId)
                    {
                        if (lastExecutorId is not null)
                        {
                            Console.WriteLine();
                        }

                        string displayName = e.ExecutorId switch
                        {
                            "Journaliste" => "JOURNALISTE",
                            "Candidat_de_Gauche" => "CANDIDAT DE GAUCHE",
                            "Candidat_de_Droite" => "CANDIDAT DE DROITE",
                            _ => e.ExecutorId ?? "Inconnu"
                        };

                        Console.ForegroundColor = e.ExecutorId switch
                        {
                            "Journaliste" => ConsoleColor.Yellow,
                            "Candidat_de_Gauche" => ConsoleColor.Red,
                            "Candidat_de_Droite" => ConsoleColor.Blue,
                            _ => ConsoleColor.White
                        };

                        Console.WriteLine($"  [{displayName}]");
                        Console.ResetColor();
                        Console.WriteLine($"  {new string('-', 50)}");
                        lastExecutorId = e.ExecutorId;
                    }

                    Console.Write(e.Update.Text);
                    break;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("================================================================");
        Console.WriteLine("  FIN DU DEBAT");
        Console.WriteLine("================================================================");
    }

    private static string PromptForTheme()
    {
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine("|           DEBAT POLITIQUE - Simulateur IA                 |");
        Console.WriteLine("|                                                           |");
        Console.WriteLine("|  3 agents IA debattent sur le theme de votre choix :      |");
        Console.WriteLine("|  - Un journaliste (animateur)                             |");
        Console.WriteLine("|  - Un candidat de gauche                                  |");
        Console.WriteLine("|  - Un candidat de droite                                  |");
        Console.WriteLine("+----------------------------------------------------------+");
        Console.WriteLine();
        Console.Write("Entrez le theme du debat : ");

        string? theme = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(theme))
        {
            Console.Write("Veuillez entrer un theme valide : ");
            theme = Console.ReadLine();
        }

        return theme;
    }
}
