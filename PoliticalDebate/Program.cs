using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PoliticalDebate;

public class Program
{
    public static async Task Main(string[] args)
    {
        string theme = args.Length > 0
            ? string.Join(" ", args)
            : PromptForTheme();

        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  DÉBAT POLITIQUE : {theme.ToUpperInvariant()}");
        Console.WriteLine("════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Erreur : La variable d'environnement OPENAI_API_KEY n'est pas définie.");
            Console.WriteLine("Définissez-la avec : export OPENAI_API_KEY=sk-...");
            Console.ResetColor();
            return;
        }

        string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

        Kernel kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(model, apiKey)
            .Build();

        // --- Définition des 3 agents ---

#pragma warning disable SKEXP0110

        ChatCompletionAgent interviewer = new()
        {
            Name = "Journaliste",
            Description = "Un journaliste politique qui anime le débat",
            Instructions = """
                Tu es un journaliste politique chevronné qui anime un débat télévisé.
                Tu poses des questions incisives et relances les candidats.
                Tu veilles à ce que chaque camp s'exprime équitablement.
                Tu commences par présenter le thème du débat, puis tu poses une question à chaque candidat.
                À chaque tour, tu reformules ou approfondis la question en fonction des réponses précédentes.
                Tu restes neutre et factuel. Tu parles en français.
                Tu t'adresses aux candidats par leur nom (Candidat de Gauche, Candidat de Droite).
                """,
            Kernel = kernel,
        };

        ChatCompletionAgent leftPolitician = new()
        {
            Name = "Candidat_de_Gauche",
            Description = "Un politicien de gauche qui défend ses idées progressistes",
            Instructions = """
                Tu es un politicien français de gauche, progressiste et engagé.
                Tu défends la justice sociale, la redistribution des richesses, les services publics,
                l'écologie, les droits des travailleurs et la solidarité.
                Tu argumentes avec passion mais reste respectueux de ton adversaire.
                Tu réponds aux questions du journaliste et tu réagis aux arguments de ton adversaire de droite.
                Tu parles en français. Tu es concis (3-4 phrases maximum par intervention).
                """,
            Kernel = kernel,
        };

        ChatCompletionAgent rightPolitician = new()
        {
            Name = "Candidat_de_Droite",
            Description = "Un politicien de droite qui défend ses idées conservatrices",
            Instructions = """
                Tu es un politicien français de droite, libéral et conservateur.
                Tu défends la liberté d'entreprise, la responsabilité individuelle, la sécurité,
                la réduction des impôts, la maîtrise de la dépense publique et l'autorité de l'État.
                Tu argumentes avec conviction mais reste respectueux de ton adversaire.
                Tu réponds aux questions du journaliste et tu réagis aux arguments de ton adversaire de gauche.
                Tu parles en français. Tu es concis (3-4 phrases maximum par intervention).
                """,
            Kernel = kernel,
        };

        // --- Configuration du débat : 3 tours x 3 agents = 9 interventions ---

        GroupChatOrchestration orchestration = new(
            new RoundRobinGroupChatManager { MaximumInvocationCount = 9 },
            interviewer,
            leftPolitician,
            rightPolitician)
        {
            ResponseCallback = (ChatMessageContent response) =>
            {
                string displayName = response.AuthorName switch
                {
                    "Journaliste" => "JOURNALISTE",
                    "Candidat_de_Gauche" => "CANDIDAT DE GAUCHE",
                    "Candidat_de_Droite" => "CANDIDAT DE DROITE",
                    _ => response.AuthorName ?? "Inconnu"
                };

                Console.ForegroundColor = response.AuthorName switch
                {
                    "Journaliste" => ConsoleColor.Yellow,
                    "Candidat_de_Gauche" => ConsoleColor.Red,
                    "Candidat_de_Droite" => ConsoleColor.Blue,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"  [{displayName}]");
                Console.ResetColor();
                Console.WriteLine($"  {new string('-', 50)}");
                Console.WriteLine($"  {response.Content}");
                Console.WriteLine();

                return ValueTask.CompletedTask;
            },
        };

        // --- Lancement du débat ---

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        Console.WriteLine("  Le débat commence...");
        Console.WriteLine();

        OrchestrationResult<string> result = await orchestration.InvokeAsync(
            $"Le thème du débat d'aujourd'hui est : \"{theme}\". Présentez le sujet et lancez le débat.",
            runtime);

        string finalResult = await result.GetValueAsync(TimeSpan.FromSeconds(120));

        await runtime.RunUntilIdleAsync();

#pragma warning restore SKEXP0110

        Console.WriteLine("════════════════════════════════════════════════════════════════");
        Console.WriteLine("  FIN DU DÉBAT");
        Console.WriteLine("════════════════════════════════════════════════════════════════");
    }

    private static string PromptForTheme()
    {
        Console.WriteLine("+==========================================================+");
        Console.WriteLine("|           DÉBAT POLITIQUE - Simulateur IA                |");
        Console.WriteLine("|                                                          |");
        Console.WriteLine("|  3 agents IA débattent sur le thème de votre choix :     |");
        Console.WriteLine("|  - Un journaliste (animateur)                            |");
        Console.WriteLine("|  - Un candidat de gauche                                 |");
        Console.WriteLine("|  - Un candidat de droite                                 |");
        Console.WriteLine("+==========================================================+");
        Console.WriteLine();
        Console.Write("Entrez le thème du débat : ");

        string? theme = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(theme))
        {
            Console.Write("Veuillez entrer un thème valide : ");
            theme = Console.ReadLine();
        }

        return theme;
    }
}
