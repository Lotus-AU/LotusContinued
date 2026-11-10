using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Options.Interface;
using VentLib.Options;
using VentLib.Options.Interfaces;
using VentLib.Options.UI;

namespace Lotus.Options;

public abstract class LotusOptionHolder : ILotusOptionHolder
{
    public abstract OptionManager OptionManager { get; }

    public List<GameOption> AllOptions { get; } = [];

    private readonly List<TabConnection> tabsListening = [];

    protected void PostInitialize()
    {
        foreach (GameOption option in AllOptions)
        {
            if (!option.Attributes.ContainsKey("Title"))
                OptionManager.Register(option, OptionLoadMode.LoadOrCreate);
        }

        foreach (TabConnection tab in tabsListening)
        {
            tab.AddOptions(AllOptions);
        }
    }

    public void AddAdditionalOption(GameOption option, int index = int.MaxValue)
    {
        if (!option.Attributes.ContainsKey("Title"))
            OptionManager.Register(option, OptionLoadMode.LoadOrCreate);

        index = Math.Clamp(index, 0, AllOptions.Count);

        AllOptions.Insert(index, option);

        foreach (TabConnection connection in tabsListening)
        {
            connection.AddOption(option, index, AllOptions);
        }
    }

    public void AddTabListener(IMainSettingTab tab)
        => AddTabListener(tab, () => 2);

    public void AddTabListener(IMainSettingTab tab, int index)
        => AddTabListener(tab, () => index);

    public void AddTabListener(IMainSettingTab tab, Func<int> index)
    {
        if (tabsListening.Any(t => t.Tab == tab))
            return;

        TabConnection connection = new(tab, index);

        tabsListening.Add(connection);

        connection.AddOptions(AllOptions);
    }

    public void RemoveTabListener(IMainSettingTab tab)
    {
        TabConnection? connection =
            tabsListening.FirstOrDefault(t => t.Tab == tab);

        if (connection == null)
            return;

        tabsListening.Remove(connection);

        foreach (GameOption option in AllOptions)
        {
            connection.RemoveOption(option);
        }
    }

    public bool IsSubscribed(IMainSettingTab tab)
        => tabsListening.Any(t => t.Tab == tab);

    private class TabConnection(IMainSettingTab tab, Func<int> addIndex)
    {
        public IMainSettingTab Tab { get; } = tab;

        public Func<int> InsertIndex { get; } = addIndex;

        public void AddOptions(List<GameOption> optionsToAdd)
        {
            List<GameOption> tabOptions = Tab.GetOptions();

            // IMPORTANT:
            // Calculate base ONCE so shifting inserts don't corrupt ordering
            int baseIndex = Math.Clamp(
                InsertIndex(),
                0,
                tabOptions.Count
            );

            for (int i = 0; i < optionsToAdd.Count; i++)
            {
                tabOptions.Insert(
                    Math.Clamp(baseIndex + i, 0, tabOptions.Count),
                    optionsToAdd[i]
                );
            }
        }

        public void AddOption(
            GameOption option,
            int relativeIndex,
            List<GameOption> ownerOptions)
        {
            List<GameOption> tabOptions = Tab.GetOptions();

            GameOption? anchor =
                ownerOptions.FirstOrDefault(o =>
                    !ReferenceEquals(o, option) &&
                    tabOptions.Contains(o));

            int insertIndex;

            if (anchor != null)
            {
                int sectionStart = tabOptions.IndexOf(anchor);

                insertIndex = Math.Clamp(
                    sectionStart + relativeIndex,
                    0,
                    tabOptions.Count
                );
            }
            else
            {
                insertIndex = Math.Clamp(
                    InsertIndex(),
                    0,
                    tabOptions.Count
                );
            }

            tabOptions.Insert(insertIndex, option);
        }

        public void RemoveOption(GameOption option)
        {
            Tab.RemoveOption(option);
        }
    }
}