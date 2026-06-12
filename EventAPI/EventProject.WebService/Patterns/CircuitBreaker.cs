namespace EventAPI.WebPlatformService.Patterns
{
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string? message) : base(message)
        {
        }
    }

    public class CircuitBreaker
    {
        private object _lock = new object();
        private readonly int _failuireThreshold;
        private readonly TimeSpan _openDuration;
        private DateTime _lastFailiureTime = DateTime.MinValue;
        private int _failuireCount;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;

        public CircuitBreaker(int failuireThreshold, TimeSpan openDuration)
        {
            _failuireThreshold = failuireThreshold;
            _openDuration = openDuration;
        }

        public CircuitBreakerState State
        {
            get
            {
                lock (_lock)
                {
                    if (_state == CircuitBreakerState.Open && (DateTime.UtcNow - _lastFailiureTime) > _openDuration)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        Console.WriteLine("CircuitBreaker HalfOpen");
                    }
                }

                return _state;
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if (State == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException("CircuitBreaker Open Exception");
            }

            try
            {
                var result = await action();

                lock (_lock)
                {
                    _failuireCount = 0;
                    _state = CircuitBreakerState.Closed;
                    Console.WriteLine("CircuitBreaker Closed");
                }

                return result;
            }
            catch (Exception)
            {
                lock (_lock)
                {
                    _failuireCount++;
                    _lastFailiureTime = DateTime.UtcNow;

                    if (_state == CircuitBreakerState.HalfOpen)
                    {
                        Console.WriteLine("CircuitBreaker HalfOpen Failure");
                        _state = CircuitBreakerState.Open;
                        Console.WriteLine("CircuitBreaker Open");

                    }
                    if (_failuireCount > _failuireThreshold)
                    {
                        Console.WriteLine("CircuitBreaker Failure Threshold Exceeded");
                        _state = CircuitBreakerState.Open;
                        Console.WriteLine("CircuitBreaker Open");
                    }
                }

                throw;
            }
        }
    }

}
