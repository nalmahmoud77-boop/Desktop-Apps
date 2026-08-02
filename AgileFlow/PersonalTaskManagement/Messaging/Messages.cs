namespace PersonalTaskManagement.Messaging
{
    public sealed record TaskMovedMessage(int TaskId, int FromColumnId, int ToColumnId, int NewIndex);

    public sealed record TaskUpdatedMessage(int TaskId);

    public sealed record TaskCreatedMessage(int TaskId, int ColumnId);

    public sealed record TaskDeletedMessage(int TaskId, int ColumnId);

    public sealed record ColumnAddedMessage(int ColumnId, int BoardId);

    public sealed record ColumnRemovedMessage(int ColumnId, int BoardId);

    public sealed record SearchChangedMessage(string Query);

    public sealed record StatusMessage(string Text);
}
