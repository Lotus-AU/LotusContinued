using System;
using System.Collections.Generic;
using VentLib.Options;
using VentLib.Options.Interfaces;
using VentLib.Options.UI;

namespace Lotus.Options.Interface;

public interface ILotusOptionHolder
{
    public List<GameOption> AllOptions { get; }

    public OptionManager OptionManager { get; }

    void AddAdditionalOption(GameOption option, int index);

    void AddTabListener(IMainSettingTab tab);

    void AddTabListener(IMainSettingTab tab, int index);

    void AddTabListener(IMainSettingTab tab, Func<int> index);

    void RemoveTabListener(IMainSettingTab tab);
}