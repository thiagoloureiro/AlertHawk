namespace AlertHawk.Metrics.API
{
    using Microsoft.AspNetCore.Mvc.ApplicationModels;

    public class GlobalRoutePrefixConvention : IApplicationModelConvention
    {
        private readonly AttributeRouteModel _routePrefix;
        public GlobalRoutePrefixConvention(string routePrefix)
        {
            _routePrefix = new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(routePrefix));
        }
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var selector in controller.Selectors)
                {
                    // If controller already has a route attribute
                    if (selector.AttributeRouteModel != null)
                    {
                        selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                            _routePrefix,
                            selector.AttributeRouteModel);
                    }
                    else
                    {
                        selector.AttributeRouteModel = _routePrefix;
                    }
                }
            }
        }
    }
}