using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace PostEdge.Aspects {
    [RequirePostSharp("PostEdge","PostEdge")]
    public interface IPostEdgeAspect: IAspect{}

    public interface IPropertyAspect:IPostEdgeAspect{}
}