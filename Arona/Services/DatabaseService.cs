using LiteDB;
using Arona.Commands;

namespace Arona.Services;

internal static class DatabaseService
{
    /// <summary>
    /// A boolean representing if database collections are currently being updated
    /// </summary>
    /// <remarks>
    /// <para>Only set by code that isn't part of a command</para>
    /// <para>Only set by code that updates collections that also are updated by discord commands</para>
    /// <para>Exceptions includes <see cref="OwnerCommands"/></para>
    /// </remarks>
    public static bool IsDatabaseUpdating { get; set; }

    /// <summary>
    /// A list of document identifiers currently being updated by something
    /// </summary>
    /// <remarks>
    /// Any document identifier in this list cannot be updated by any processes other than the one who called '<see cref="AddToList"/>'
    /// </remarks>
    private static List<string> ActiveDocumentUpdates { get; } = [];

    /// <summary>
    /// Adds the specified document identifier to '<see cref="ActiveDocumentUpdates"/>'
    /// effectively locking the document from being updated by another process
    /// </summary>
    /// <param name="documentId"><see cref="BsonIdAttribute"/> (BsonId) of a document</param>
    private static void AddToList(string documentId) => ActiveDocumentUpdates.Add(documentId);

    /// <summary>
    /// Removes the specified document identifier from '<see cref="ActiveDocumentUpdates"/>',
    /// making the document writable again by other processes
    /// </summary>
    /// <param name="documentId"><see cref="BsonIdAttribute"/> (BsonId) of a document</param>
    public static void RemoveFromList(string documentId) => ActiveDocumentUpdates.Remove(documentId);

    /// <summary>
    /// Wait until (<see cref="IsDatabaseUpdating"/> == false)
    /// </summary>
    public static async Task WaitForUpdateAsync()
    {
        while (IsDatabaseUpdating)
        {
            Console.WriteLine("Waiting for update to finish...");

            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Wait until a document identifier no longer exists in '<see cref="ActiveDocumentUpdates"/>'
    /// </summary>
    public static async Task WaitForWriteAsync(string documentId)
    {
        while (ActiveDocumentUpdates.Contains(documentId))
        {
            Console.WriteLine("Waiting for database write for '" + documentId + "' to finish...");

            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Wait until '<see cref="ActiveDocumentUpdates"/>' is empty
    /// </summary>
    public static async Task WaitForAllWritesAsync()
    {
        while (ActiveDocumentUpdates.Count > 0)
        {
            Console.WriteLine("Waiting for database write to finish...");

            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Represents a key used to manage write access to a specific document in the database.
    /// </summary>
    /// <remarks>This class ensures that the document associated with the specified <see cref="DocumentId"/> is
    /// registered for write operations when the object is created and unregistered when disposed. Use this class within a
    /// '<c>using</c>' statement to ensure proper cleanup of resources.</remarks>
    public class DatabaseWriteKey : IDisposable
    {
        public string DocumentId { get; set; }

        /// <param name="documentId">Document identifier</param>
        public DatabaseWriteKey(string documentId)
        {
            DocumentId = documentId;

            AddToList(DocumentId);
        }

        public void Dispose()
        {
            RemoveFromList(DocumentId);
            GC.SuppressFinalize(this);
        }
    }
}