using System.ComponentModel;
using FluentAssertions;
using NUnit.Framework;
using PostEdge.Aspects;
using PostSharp;

namespace PostEdge.Aspects {
    public class NotifyPropertyChangedTests {
        [Test]
        public void PropertyChanges_Fire_PropertyChangedEvent() {
            var source = new MockNotifyingObject{Flag = false};
            var target = new MockNotifyingObject();
            var notifier = Post.Cast<MockNotifyingObject,INotifyPropertyChanged>(source);

            notifier.PropertyChanged += (s, args) => {
                if (args.PropertyName == "Id") {
                    target.Id = source.Id;
                }
                if (args.PropertyName == "Name") {
                    target.Name = source.Name;
                }
                if(args.PropertyName == "Flag") {
                    target.Flag = source.Flag;
                }
            };
            source.Id = 9999;
            source.Name = "John Smith";
            source.Flag = false;
            target.ShouldHave().Properties(x=>x.Id,x=>x.Name).EqualTo(source);
            target.Flag.Should().Be(!source.Flag);
        }
        [NotifyPropertyChanged]
        private class MockNotifyingObject {
            private bool _flag = true;
            public int Id { get; set; }
            public string Name { get; set; }            
            public bool Flag {
                get { return _flag; }
                set { _flag = value; }
            }

            [NoChangeNotification]
            public bool? NoChangeNotification { get; set; }
        }

        [NotifyPropertyChanged]
        public class MockNotifyingObjectWithINPC:INotifyPropertyChanged {
            public event PropertyChangedEventHandler PropertyChanged;
            private bool _flag = true;
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Flag {
                get { return _flag; }
                set { _flag = value; }
            }

            [NoChangeNotification]
            public bool? NoChangeNotification { get; set; }
        }
    }
}
