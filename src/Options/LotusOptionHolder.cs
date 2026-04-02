using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Options.Interface;
using VentLib.Options;
using VentLib.Options.Interfaces;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;

namespace Lotus.Options;

public abstract class LotusOptionHolder: ILotusOptionHolder
{
    public abstract OptionManager OptionManager { get; }

    public List<GameOption> AllOptions { get; } = [];
    private List<TabConnection> tabsListening = [];

    protected void PostInitialize()
    {
        AllOptions.ForEach((o, i) =>
        {
            if (!o.Attributes.ContainsKey("Title")) OptionManager.Register(o, OptionLoadMode.LoadOrCreate);
            tabsListening.ForEach(t => t.AddOption(o, i));
        });
    }

    /// <summary>
    /// Adds additional options to be registered when this group of options is loaded. This is mostly used for ordering
    /// in the main menu, as options passed in here will be rendered along with this group.
    /// </summary>
    /// <param name="option">Option to render</param>
    /// <param name="index">The position to add the option at.</param>
    public void AddAdditionalOption(GameOption option, int index = int.MaxValue)
    {
        if (!option.Attributes.ContainsKey("Title")) OptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        if (index < 0) index = 0;
        else if (index > AllOptions.Count) index = AllOptions.Count;
        AllOptions.Insert(index, option);
        tabsListening.ForEach(t => t.AddOption(option, index));
    }

    public void AddTabListener(IMainSettingTab tab) => AddTabListener(tab, () => 2);

    public void AddTabListener(IMainSettingTab tab, int index) => AddTabListener(tab, () => index);

    public void AddTabListener(IMainSettingTab tab, Func<int> index)
    {
        if (tabsListening.Any(tl => tl.Tab == tab)) return;
        tabsListening.Add(new TabConnection(tab, index));
        AllOptions.ForEach(tab.AddOption);
    }

    public void RemoveTabListener(IMainSettingTab tab)
    {
        TabConnection? tabConnection = tabsListening.FirstOrDefault(tl => tl.Tab == tab);
        if (tabConnection == null) return;
        tabsListening.Remove(tabConnection);
        AllOptions.ForEach(tabConnection.RemoveOption);
    }

    public bool IsSubscribed(IMainSettingTab tab) => tabsListening.Any(t => t.Tab == tab);

    class TabConnection(IMainSettingTab tab, Func<int> addIndex)
    {
        public IMainSettingTab Tab { get; } = tab;
        public Func<int> InsertIndex { get; } = addIndex;

        public void AddOption(GameOption option, int index)
        {
            List<GameOption> options = Tab.GetOptions();
            options.Insert(Math.Clamp(InsertIndex() + index, 0, options.Count), option);
        }

        public void RemoveOption(GameOption option)
        {
            Tab.RemoveOption(option);
        }

        public void RemoveOption(int index)
        {
            List<GameOption> options = Tab.GetOptions();
            options.RemoveAt(Math.Clamp(InsertIndex() + index, 0, options.Count));
        }
    }
}