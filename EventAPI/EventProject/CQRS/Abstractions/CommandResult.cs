namespace EventAPI.CQRS.Abstractions
{

    public enum CommandStatus
    {
        Success,
        ValidationError,
        NotFound,
        Error
    }

    public class CommandResult
    {
        public CommandStatus Status { get; }

        public bool Success => Status == CommandStatus.Success;

        public IReadOnlyList<string> Errors { get; }

        protected CommandResult(CommandStatus status, IReadOnlyList<string> errors)
        {
            Status = status;
            Errors = errors;
        }

        public static CommandResult Ok() =>
            new CommandResult(CommandStatus.Success, Array.Empty<string>());

        public static CommandResult ValidationFailed(IEnumerable<string> errors) =>
            new CommandResult(CommandStatus.ValidationError, errors.ToList());

        public static CommandResult NotFound(string message) =>
            new CommandResult(CommandStatus.NotFound, new List<string> { message });

        public static CommandResult Error(string message) =>
            new CommandResult(CommandStatus.Error, new List<string> { message });
    }

    public sealed class CommandResult<T> : CommandResult
    {
        public T? Data { get; }

        private CommandResult(CommandStatus status, T? data, IReadOnlyList<string> errors)
            : base(status, errors)
        {
            Data = data;
        }

        public static CommandResult<T> Ok(T data) =>
            new CommandResult<T>(CommandStatus.Success, data, Array.Empty<string>());

        public static new CommandResult<T> ValidationFailed(IEnumerable<string> errors) =>
            new CommandResult<T>(CommandStatus.ValidationError, default, errors.ToList());

        public static new CommandResult<T> NotFound(string message) =>
            new CommandResult<T>(CommandStatus.NotFound, default, new List<string> { message });

        public static new CommandResult<T> Error(string message) =>
            new CommandResult<T>(CommandStatus.Error, default, new List<string> { message });
    }
}
