namespace PostEdge.Reactive.ChangeTracking {
    using ReactiveUI;

    public static class ChangeTrackerMixin {
        public static void MarkAsChanged<TValue>(this IChangeTracker changeTracker, string propertyName, TValue value) {
            var change = new ObservedChange<object, TValue> {
                Sender = changeTracker.Target,
                PropertyName = propertyName,
                Value = value
            };
            changeTracker.MarkAsChanged(change);
        }
    }
}