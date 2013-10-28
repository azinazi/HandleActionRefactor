using FluentValidation;
using FluentValidation.Mvc;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using HandleActionRefactor.Controllers;
using SchoStack.Web;
using SchoStack.Web.Conventions;
using SchoStack.Web.Conventions.Core;
using SchoStack.Web.FluentValidation;
using StructureMap;

namespace HandleActionRefactor
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new StructureMapValidatorFactory()));

            HtmlConventionFactory.Add(new DefaultHtmlConventions());
            HtmlConventionFactory.Add(new DataAnnotationHtmlConventions());
            HtmlConventionFactory.Add(new FluentValidationHtmlConventions(new FluentValidatorFinder(DependencyResolver.Current)));

            ObjectFactory.Container.Configure(x =>
            {
                x.Scan(y =>
                {
                    y.TheCallingAssembly();
                    y.AssemblyContainingType<IInvoker>();
                    y.WithDefaultConventions();
                    y.ConnectImplementationsToTypesClosing(typeof (IValidator<>));
                    y.ConnectImplementationsToTypesClosing(typeof (IHandler<>));
                    y.ConnectImplementationsToTypesClosing(typeof (IHandler<,>));
                    y.ConnectImplementationsToTypesClosing(typeof(ICommandHandler<>));
                });
                x.For<IInvoker>().Use(sm => new Invoker(z => sm.TryGetInstance(z)));
                x.FillAllPropertiesOfType<IInvoker>();
            });
        }
    }

    public class StructureMapValidatorFactory : ValidatorFactoryBase
    {
        public override IValidator CreateInstance(Type validatorType)
        {
            return ObjectFactory.TryGetInstance(validatorType) as IValidator;
        }
    }
}