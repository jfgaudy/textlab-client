using System;
using System.Threading.Tasks;
using System.Linq;
using TextLabClient.Services;

namespace TextLabClient
{
    public class TestDocumentCreation
    {
        public static async Task RunTest()
        {
            var apiService = new TextLabApiService();
            
            Console.WriteLine("🔍 Test de diagnostic de création de documents");
            Console.WriteLine("==================================================");
            
            try
            {
                // Test de connexion d'abord
                Console.WriteLine("\n🔗 Test de connexion à l'API...");
                var health = await apiService.TestConnectionAsync();
                if (health == null)
                {
                    Console.WriteLine("❌ Impossible de se connecter à l'API");
                    return;
                }
                Console.WriteLine("✅ Connexion API OK");
                
                // Test 1: Document avec category = null
                Console.WriteLine("\n📝 Test 1: Création avec category = null");
                var doc1 = await apiService.CreateDocumentAsync(
                    title: "Test Document Root " + DateTime.Now.ToString("HH:mm:ss"),
                    content: "# Test Document\n\nCeci est un test avec category = null",
                    repositoryId: null, // Repository par défaut
                    category: null,
                    visibility: "private",
                    createdBy: "TestClient"
                );
                
                if (doc1 != null)
                {
                    Console.WriteLine($"✅ Document créé: {doc1.Id}");
                    Console.WriteLine($"   Title: {doc1.Title}");
                    Console.WriteLine($"   GitPath: {doc1.GitPath ?? "null"}");
                    Console.WriteLine($"   Category: {doc1.Category ?? "null"}");
                }
                else
                {
                    Console.WriteLine("❌ Échec création document 1");
                }
                
                // Test 2: Document avec category = "test"
                Console.WriteLine("\n📝 Test 2: Création avec category = 'test'");
                var doc2 = await apiService.CreateDocumentAsync(
                    title: "Test Document Category " + DateTime.Now.ToString("HH:mm:ss"),
                    content: "# Test Document\n\nCeci est un test avec category = 'test'",
                    repositoryId: null,
                    category: "test",
                    visibility: "private",
                    createdBy: "TestClient"
                );
                
                if (doc2 != null)
                {
                    Console.WriteLine($"✅ Document créé: {doc2.Id}");
                    Console.WriteLine($"   Title: {doc2.Title}");
                    Console.WriteLine($"   GitPath: {doc2.GitPath ?? "null"}");
                    Console.WriteLine($"   Category: {doc2.Category ?? "null"}");
                }
                else
                {
                    Console.WriteLine("❌ Échec création document 2");
                }
                
                // Test 3: Document avec category = "internal"
                Console.WriteLine("\n📝 Test 3: Création avec category = 'internal'");
                var doc3 = await apiService.CreateDocumentAsync(
                    title: "Test Document Internal " + DateTime.Now.ToString("HH:mm:ss"),
                    content: "# Test Document\n\nCeci est un test avec category = 'internal'",
                    repositoryId: null,
                    category: "internal",
                    visibility: "private",
                    createdBy: "TestClient"
                );
                
                if (doc3 != null)
                {
                    Console.WriteLine($"✅ Document créé: {doc3.Id}");
                    Console.WriteLine($"   Title: {doc3.Title}");
                    Console.WriteLine($"   GitPath: {doc3.GitPath ?? "null"}");
                    Console.WriteLine($"   Category: {doc3.Category ?? "null"}");
                }
                else
                {
                    Console.WriteLine("❌ Échec création document 3");
                }
                
                // Récupérer la liste des documents pour voir leur structure
                Console.WriteLine("\n📋 Liste des documents récents");
                var documents = await apiService.GetDocumentsAsync();
                
                if (documents != null && documents.Count > 0)
                {
                    var recentDocs = documents
                        .Where(d => d.Title.StartsWith("Test Document"))
                        .OrderByDescending(d => d.CreatedAt)
                        .Take(5);
                    
                    foreach (var doc in recentDocs)
                    {
                        Console.WriteLine($"   📄 {doc.Title}");
                        Console.WriteLine($"      GitPath: {doc.GitPath ?? "null"}");
                        Console.WriteLine($"      Category: {doc.Category ?? "null"}");
                        Console.WriteLine($"      Created: {doc.CreatedAt}");
                        Console.WriteLine();
                    }
                }
                
                Console.WriteLine("\n🎯 Diagnostic terminé !");
                Console.WriteLine("Maintenant, vérifiez dans GitHub où ces documents sont réellement stockés.");
                Console.WriteLine("Appuyez sur Entrée pour fermer...");
                Console.ReadLine();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur pendant le test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine("Appuyez sur Entrée pour fermer...");
                Console.ReadLine();
            }
        }
    }
} 