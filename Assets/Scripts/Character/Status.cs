using System;
using System.Collections.Generic;

public class Status
{
    private string _name;
        
    private int _hp;
    private int _mp;
    private int _atk;
    private int _def;
    private int _spd;
    private int _kd;
    private int _kr;
    private int _gold;

    private string Name {get => _name; set => _name = value; }

    public int Hp { get => _hp; set => _hp = value; }
    public int Mp { get => _mp; set => _mp = value; }
    public int Atk { get => _atk; set => _atk = value; }
    public int Def { get => _def; set => _def = value; }
    public int Spd { get => _spd; set => _spd = value; }
    public int Kd { get => _kd; set => _kd = value; }
    public int Kr { get => _kr; set => _kr = value; }
    public int Gold { get => _gold; set => _gold = value; }
}
