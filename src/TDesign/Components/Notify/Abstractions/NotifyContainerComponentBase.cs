﻿using TDesign.Abstractions;

namespace TDesign.Notification;
/// <summary>
/// 表示通知提示的容器基类组件。
/// </summary>
/// <typeparam name="TNotifyService">通知服务的类型。</typeparam>
/// <typeparam name="TConfiguration">配置的类型。</typeparam>
public abstract class NotifyContainerComponentBase<TNotifyService, TConfiguration> : BlazorComponentBase, IContainerComonent, IDisposable
    where TNotifyService : INotifyService<TConfiguration>
    where TConfiguration : NotifyConfigurationBase
{

    [Inject] TNotifyService? NotifyService { get; set; }

    protected Dictionary<OneOf<Placement, (int offsetX, int offsetY)>, List<TConfiguration>> ConfigurationDics = new();



    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        NotifyService!.OnShowing += NotifyService_OnShowing;
    }

    /// <summary>
    /// Notifies the message on showing.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A Task.</returns>
    private async Task NotifyService_OnShowing(TConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        await AddItem(configuration);
        await this.Refresh();
    }

    /// <summary>
    /// Adds the item.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A Task.</returns>
    protected async Task AddItem(TConfiguration configuration)
    {
        var key = configuration.Placement;
        if (ConfigurationDics.ContainsKey(key))
        {
            ConfigurationDics[key].Add(configuration);
        }
        else
        {
            ConfigurationDics.Add(key, new() { configuration });
        }

        await this.Refresh();

        await OnTimeoutRemove();

        Task OnTimeoutRemove()
        {
            using Timer timer = new(async (state) =>
            {
                await RemoveItem((TConfiguration)state);
            }, configuration, configuration.Delay ??= Timeout.Infinite, Timeout.Infinite);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Removes the item.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A Task.</returns>
    protected Task RemoveItem(TConfiguration configuration)
    {
        var key = configuration.Placement;
        if (ConfigurationDics.ContainsKey(key))
        {
            ConfigurationDics[key].Remove(configuration);

            if (!ConfigurationDics[key].Any())
            {
                ConfigurationDics.Remove(key);
            }
        }
        return this.Refresh();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (NotifyService is not null)
        {
            NotifyService.OnShowing -= NotifyService_OnShowing;
        }
    }
}
