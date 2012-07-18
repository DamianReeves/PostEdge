namespace PostEdge.Reactive.ChangeTracking {
    using System;
    using System.ComponentModel;

    using ReactiveUI;

    public interface IChangeTracker: IChangeTracking {
        event EventHandler AcceptedChanges;
        event CancelEventHandler AcceptingChanges;

        void MarkAsChanged<TValue>(IObservedChange<object, TValue> change);
        IReactiveNotifyPropertyChanged Target { get; }
    }

    public interface IRevertibleChangeTracker:IChangeTracker,IRevertibleChangeTracking {
        
    }
}