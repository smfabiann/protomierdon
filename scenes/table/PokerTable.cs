using Godot;
using System;

public partial class PokerTable : StaticBody3D
{
    [Export] public Marker3D CornerRight {get; private set;}
    [Export] public Marker3D CornerLeft {get; private set;}
}
