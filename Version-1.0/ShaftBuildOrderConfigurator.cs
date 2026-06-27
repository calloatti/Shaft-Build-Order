using Bindito.Core;
using Timberborn.ModularShafts;
using Timberborn.TemplateInstantiation;
using Timberborn.TemplateSystem;

namespace Calloatti.ShaftBuildOrder
{
  [Context("Game")]
  public class ShaftBuildOrderConfigurator : Configurator
  {
    protected override void Configure()
    {
      Bind<ShaftDirectionVisualizer>().AsTransient();
      Bind<ShaftBuildOrderBlocker>().AsTransient();

      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule()
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();

      // Inject our custom one-way blocker and visualizer
      builder.AddDecorator<ModularShaft, ShaftBuildOrderBlocker>();
      builder.AddDecorator<ModularShaft, ShaftDirectionVisualizer>();

      return builder.Build();
    }
  }
}