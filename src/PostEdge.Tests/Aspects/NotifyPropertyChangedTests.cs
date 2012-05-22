﻿using System.ComponentModel;
using FluentAssertions;
using NUnit.Framework;
using PostEdge.Aspects;
using PostSharp;

namespace PostEdge.Aspects {
    public class NotifyPropertyChangedTests {
        [Test]
        public void PropertyChanges_Fire_PropertyChangedEvent() {
            var source = new MockNotifyingObject();
            var target = new MockNotifyingObject();
            var notifier = Post.Cast<MockNotifyingObject,INotifyPropertyChanged>(source);
            notifier.PropertyChanged += (s, args) => {
                if (args.PropertyName == "Id") {
                    target.Id = source.Id;
                }
                if (args.PropertyName == "Name") {
                    target.Name = source.Name;
                }
            };
            source.Id = 9999;
            source.Name = "John Smith";
            target.ShouldHave().Properties(x=>x.Id,x=>x.Name).EqualTo(source);
        }
        [NotifyPropertyChanged]
        private class MockNotifyingObject {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [NotifyPropertyChanged]
        public class MockNotifyingObjectWithINPC:INotifyPropertyChanged {
            public event PropertyChangedEventHandler PropertyChanged;
            public int Id { get; set; }
            public string Name { get; set; }            
        }
    }
}