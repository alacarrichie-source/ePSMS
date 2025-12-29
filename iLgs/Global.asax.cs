using iLgs.Agents.Services;
using iLgs.App_Start;
using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.AIRs_;
using iLgs.Services.AllFields;
using iLgs.Services.AuditLog_;
using iLgs.Services.Codes;
using iLgs.Services.CustodianDisposal_;
using iLgs.Services.CustodianIirup;
using iLgs.Services.CustodianReports;
using iLgs.Services.CustodianUploads;
using iLgs.Services.DollarRate_;
using iLgs.Services.Items;
using iLgs.Services.Logs;
using iLgs.Services.ParIcs;
using iLgs.Services.PoIssuance;
using iLgs.Services.PropertyCard;
using iLgs.Services.PurchaseOrder;
using iLgs.Services.PurchaseRequest;
using iLgs.Services.Requisition;
using iLgs.Services.RPC;
using iLgs.Services.StockCards;
using iLgs.Services.Uploads;
using iLgs.Services.Validators;
using iLgs.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace iLgs
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register your DbContext (EF6)
            services.AddScoped<AppManEntities>();
            services.AddScoped<IAppManEntitiesFactory, AppManEntitiesFactory>();

            //services.AddSingleton<IAppManEntitiesFactory, AppManEntitiesFactory>();
            //services.AddScoped<AppManEntities>(sp => new AppManEntities());

            //// Identity stores
            //services.AddScoped<IUserStore<ApplicationUser>>(sp =>
            //    new UserStore<ApplicationUser>(sp.GetRequiredService<AppManEntities>()));

            ////services.AddScoped<IRoleStore<IdentityRole>>(sp =>
            ////    new RoleStore<IdentityRole>(sp.GetRequiredService<AppManEntities>()));

            //// Managers
            ////services.AddScoped<ApplicationUserManager>();
            ////services.AddScoped<ApplicationSignInManager>();

            //services.AddScoped<ApplicationUserManager>(sp =>
            //{
            //    var store = sp.GetRequiredService<IUserStore<ApplicationUser>>();
            //    var manager = new ApplicationUserManager(store);

            //    // configure token provider
            //    var dataProtectionProvider = new Microsoft.Owin.Security.DataProtection.DpapiDataProtectionProvider("MyApp");
            //    manager.UserTokenProvider =
            //        new DataProtectorTokenProvider<ApplicationUser>(
            //            dataProtectionProvider.Create("ASP.NET Identity"))
            //        {
            //            TokenLifespan = TimeSpan.FromHours(1)
            //        };

            //    return manager;
            //});

            // register api controllers
            var controllers = typeof(MvcApplication).Assembly
                .GetTypes()
                .Where(type => typeof(ApiController).IsAssignableFrom(type) && !type.IsAbstract);

            foreach (var controller in controllers)
            {
                services.AddTransient(controller);
            }

            // Register services and dependencies
            // AGENTS
            services.AddScoped<IServiceAgent, ServiceAgent>();

            // AIRS
            services.AddScoped<IAirInvoiceService, AirInvoiceService>();
            services.AddScoped<IAirItemExtnOtherService, AirItemExtnOtherService>();
            services.AddScoped<IAirItemExtnVehicleService, AirItemExtnVehicleService>();
            services.AddScoped<IAirItemExtnService, AirItemExtnService>();
            services.AddScoped<IAirItemExtnAbstractService, AirItemExtnAbstractService>();
            services.AddScoped<IAirItemService, AirItemService>();
            services.AddScoped<IAirItemAbstractService, AirItemAbstractService>();
            services.AddScoped<IAirService, AirService>();
            services.AddScoped<IAirAbstractService, AirAbstractService>();
            services.AddScoped<IAirUploadService, AirUploadService>();

            // ALLFIELDS
            services.AddScoped<IAllFieldService, AllFieldService>();
            services.AddScoped<IAllFieldsValidator, AllFieldsValidator>();

            // AUDIT LOG
            services.AddScoped<IAuditLogService, AuditLogService>();

            // CODES
            services.AddScoped<ICodextnService, CodextnService>();
            services.AddScoped<IAnnexDService, AnnexDService>();
            services.AddScoped<IBudgetService, BudgetService>();
            services.AddScoped<ILocationBudgetService, LocationBudgetService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IPriceCapService, PriceCapService>();
            services.AddScoped<ISemiExpendableService, SemiExpendableService>();
            services.AddScoped<IPsCategoryService, PsCategoryService>();

            // CUSTODIAN DISPOSAL
            services.AddScoped<ICustodianDisposalItemService, CustodianDisposalItemService>();
            services.AddScoped<ICustodianDisposalService, CustodianDisposalService>();

            // CUSTODIAN IIRUP
            services.AddScoped<ICustodianIirupItemService, CustodianIirupItemService>();
            services.AddScoped<ICustodianIirupService, CustodianIirupService>();

            // CUSTODIAN REPORTS
            services.AddScoped<ICustodianReportBldgItemPhaseService, CustodianReportBldgItemPhaseService>();
            services.AddScoped<ICustodianReportBldgItemService, CustodianReportBldgItemService>();
            services.AddScoped<ICustodianReportItemIssuanceAreService, CustodianReportItemIssuanceAreService>();
            services.AddScoped<ICustodianReportItemIssuanceIcsService, CustodianReportItemIssuanceIcsService>();
            services.AddScoped<ICustodianReportItemIssuanceMrService, CustodianReportItemIssuanceMrService>();
            services.AddScoped<ICustodianReportItemIssuanceParService, CustodianReportItemIssuanceParService>();
            services.AddScoped<ICustodianReportItemIssuanceRpcPpeService, CustodianReportItemIssuanceRpcPpeService>();
            services.AddScoped<ICustodianReportItemIssuanceService, CustodianReportItemIssuanceService>();
            services.AddScoped<ICustodianReportItemPpeService, CustodianReportItemPpeService>();
            services.AddScoped<ICustodianReportItemPpeValidator, CustodianReportItemPpeValidator>();
            services.AddScoped<ICustodianReportItemService, CustodianReportItemService>();
            services.AddScoped<ICustodianReportItemStockService, CustodianReportItemStockService>();
            services.AddScoped<ICustodianReportItemStockValidator, CustodianReportItemStockValidator>();
            services.AddScoped<ICustodianReportItemVehicleService, CustodianReportItemVehicleService>();
            services.AddScoped<ICustodianReportItemVehicleValidator, CustodianReportItemVehicleValidator>();
            services.AddScoped<ICustodianReportLandItemService, CustodianReportLandItemService>();
            services.AddScoped<ICustodianReportService, CustodianReportService>();
            services.AddScoped<ICustodianReportSubmitForCountService, CustodianReportSubmitForCountService>();            
            services.AddScoped<ICustodianReportValidator, CustodianReportValidator>();

            // CUSTODIAN UPLOADS
            services.AddScoped<ICustodianDeptUploadService, CustodianDeptUploadService>();
            services.AddScoped<ICustodianBldgUploadService, CustodianBldgUploadService>();
            services.AddScoped<ICustodianIirupUploadService, CustodianIirupUploadService>();
            services.AddScoped<ICustodianLandUploadService, CustodianLandUploadService>();
            services.AddScoped<ICustodianReportUploadService, CustodianReportUploadService>();            

            // DOLLAR RATE
            services.AddScoped<IDollarRateService, DollarRateService>();

            // ITEMS
            services.AddScoped<IItemCodeRequestService, ItemCodeRequestService>();
            services.AddScoped<IItemCodeService, ItemCodeService>();
            services.AddScoped<IItemTypeExclusionService, ItemTypeExclusionService>();
            services.AddScoped<IItemTypeService, ItemTypeService>();
            services.AddScoped<IItemUploadService, ItemUploadService>();

            // NOTIFICATIONS                
            services.AddScoped<INotificationMessageService, NotificationMessageService>();
            services.AddScoped<INotificationMessageStatusService, NotificationMessageStatusService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationUserService, NotificationUserService>();

            // PAR/ICS
            services.AddScoped<IIcsParItemService, IcsParItemService>();
            services.AddScoped<IIcsParSharedService, IcsParSharedService>();
            services.AddScoped<IIcsParService, IcsParService>();
            services.AddScoped<IIcsSharedService, IcsSharedService>();
            services.AddScoped<IIcsService, IcsService>();
            services.AddScoped<IParIcsUploadService, ParIcsUploadService>();
            services.AddScoped<IParItemService, ParItemService>();
            services.AddScoped<IParService, ParService>();

            // PO-ISSUANCE
            services.AddScoped<IPoIssuanceService, PoIssuanceService>();
            services.AddScoped<IPoIssuanceUploadService, PoIssuanceUploadService>();

            // PROPERTY CARD
            services.AddScoped<IPropertyCardService, PropertyCardService>();
            services.AddScoped<IPropertyCardValidator, PropertyCardValidator>();
            services.AddScoped<IPsCardItemExtnAddCostService, PsCardItemExtnAddCostService>();
            services.AddScoped<IPsCardItemExtnBldgService, PsCardItemExtnBldgService>();
            services.AddScoped<IPsCardItemExtnBldgValidator, PsCardItemExtnBldgValidator>();
            services.AddScoped<IPsCardItemExtnLandService, PsCardItemExtnLandService>();
            services.AddScoped<IPsCardItemExtnLandValidator, PsCardItemExtnLandValidator>();
            services.AddScoped<IPsCardItemExtnOtherService, PsCardItemExtnOtherService>();
            services.AddScoped<IPsCardItemExtnOtherValidator, PsCardItemExtnOtherValidator>();
            services.AddScoped<IPsCardItemExtnSharedService, PsCardItemExtnSharedService>();
            services.AddScoped<IPsCardItemExtnService, PsCardItemExtnService>();
            services.AddScoped<IPsCardItemExtnUpdateService, PsCardItemExtnUpdateService>();
            services.AddScoped<IPsCardItemExtnValidator, PsCardItemExtnValidator>();
            services.AddScoped<IPsCardItemExtnVehicleRepairService, PsCardItemExtnVehicleRepairService>();
            services.AddScoped<IPsCardItemExtnVehicleService, PsCardItemExtnVehicleService>();
            services.AddScoped<IPsCardItemExtnVehicleValidator, PsCardItemExtnVehicleValidator>();
            //services.AddScoped<IPsCardItemIssuanceService, PsCardItemIssuanceService>();
            services.AddScoped<IPsCardItemService, PsCardItemService>();
            services.AddScoped<IPsCardItemTransactionService, PsCardItemTransactionService>();
            services.AddScoped<IPsCardItemTransferIssuanceService, PsCardItemTransferIssuanceService>();
            services.AddScoped<IPsCardItemTransferItemBldgService, PsCardItemTransferItemBldgService>();
            services.AddScoped<IPsCardItemTransferItemLandService, PsCardItemTransferItemLandService>();
            services.AddScoped<IPsCardItemTransferItemOtherService, PsCardItemTransferItemOtherService>();
            services.AddScoped<IPsCardItemTransferItemSharedService, PsCardItemTransferItemSharedService>();
            services.AddScoped<IPsCardItemTransferItemService, PsCardItemTransferItemService>();
            services.AddScoped<IPsCardItemTransferItemVehicleService, PsCardItemTransferItemVehicleService>();
            services.AddScoped<IPsCardItemValidator, PsCardItemValidator>();
            services.AddScoped<IPsCardItemTransferService, PsCardItemTransferService>();
            services.AddScoped<IPsCardSharedService, PsCardSharedService>();
            services.AddScoped<IPsCardService, PsCardService>();

            // PURCHASE ORDER
            services.AddScoped<IOrderItemSharedService, OrderItemSharedService>();
            services.AddScoped<IOrderItemService, OrderItemService>();
            services.AddScoped<IOrderItemUnitGroupDescriptionItemSharedService, OrderItemUnitGroupDescriptionItemSharedService>();
            services.AddScoped<IOrderItemUnitGroupDescriptionItemService, OrderItemUnitGroupDescriptionItemService>();
            services.AddScoped<IOrderItemUnitGroupDescriptionService, OrderItemUnitGroupDescriptionService>();
            services.AddScoped<IOrderItemUnitGroupService, OrderItemUnitGroupService>();
            services.AddScoped<IOrderSharedService, OrderSharedService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderUploadService, OrderUploadService>();

            // PURCHASE REQUEST
            services.AddScoped<IRequestItemService, RequestItemService>();
            services.AddScoped<IRequestItemUnitGroupDescriptionItemService, RequestItemUnitGroupDescriptionItemService>();
            services.AddScoped<IRequestItemUnitGroupDescriptionService, RequestItemUnitGroupDescriptionService>();
            services.AddScoped<IRequestItemUnitGroupService, RequestItemUnitGroupService>();
            services.AddScoped<IRequestService, RequestService>();

            // REQUESITION
            services.AddScoped<IRisItemService, RisItemService>();
            services.AddScoped<IRisItemUnitGroupDescriptionItemService, RisItemUnitGroupDescriptionItemService>();
            services.AddScoped<IRisItemUnitGroupDescriptionService, RisItemUnitGroupDescriptionService>();
            services.AddScoped<IRisItemUnitGroupService, RisItemUnitGroupService>();
            services.AddScoped<IRisService, RisService>();

            // RPC
            services.AddScoped<IRpciItemService, RpciItemService>();
            services.AddScoped<IRpciService, RpciService>();
            services.AddScoped<IRpcPpeItemService, RpcPpeItemService>();
            services.AddScoped<IRpcPpeService, RpcPpeService>();

            // STOCK CARDS
            services.AddScoped<IStockCardService, StockCardService>();
            services.AddScoped<IStockCardValidator, StockCardValidator>();

            // UPLOADS
            services.AddScoped<IAddCostUploadService, AddCostUploadService>();
            services.AddScoped<IItemCodeRequestUploadService, ItemCodeRequestUploadService>();
            services.AddScoped<IUploadService, UploadService>();

            // VALIDATORS
            services.AddScoped<IRisItemUnitGroupValidator, RisItemUnitGroupValidator>();
            services.AddScoped<IRisItemValidator, RisItemValidator>();
            services.AddScoped<IRisValidator, RisValidator>();

            // OTHERS
            services.AddScoped<IAccountableOfficerService, AccountableOfficerService>();
            services.AddScoped<ICardUploadService, CardUploadService>();
            services.AddScoped<IDepartmentUserService, DepartmentUserService>();
            services.AddScoped<IDirectoryService, DirectoryService>();
            services.AddScoped(typeof(IExceptionService<>), typeof(ExceptionService<>));
            services.AddScoped<ILoggingService, LoggingService>();
            services.AddScoped<IRpceffoppeItemService, RpceffoppeItemService>();
            services.AddScoped<IRpceffoppeService, RpceffoppeService>();
            services.AddScoped<IRsmiService, RsmiService>();
            services.AddScoped<ICreateAndLogExceptions, CreateAndLogExceptions>();
            services.AddScoped<IItemCodeService, ItemCodeService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRsmiService, RsmiService>();
            services.AddScoped<Func<string, UploadService>>(sp => subDir => new UploadService(sp.GetRequiredService<AppManEntities>(), sp.GetRequiredService<AppManEntitiesFactory>(), subDir));

            // Register controllers
            var controllerTypes = typeof(MvcApplication).Assembly.GetTypes()
                .Where(t => typeof(IController).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (var controllerType in controllerTypes)
            {
                services.AddTransient(controllerType);
            }

            // Build the service provider
            var provider = services.BuildServiceProvider();
            // 🔧 Set MVC dependency resolver
            DependencyResolver.SetResolver(new DefaultDependencyResolver(provider));


            // 🔧 Set Web API dependency resolver
            GlobalConfiguration.Configuration.DependencyResolver = new DefaultWebApiDependencyResolver(provider);
            ControllerBuilder.Current.SetControllerFactory(new ServiceProviderControllerFactory(provider));

            // 2025.10.04
            var serializer = GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings;
            serializer.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        }
    }
}
