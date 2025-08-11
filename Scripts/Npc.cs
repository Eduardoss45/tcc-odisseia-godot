using Godot;
using System;

public partial class Npc : CharacterBody2D
{
    private Node? dialogic;
    private bool isConnected = false;

    public override void _Ready()
    {
        dialogic = GetNodeOrNull("/root/Dialogic");
        if (dialogic == null)
        {
            GD.PrintErr("Dialogic não encontrado em /root/Dialogic");
            return;
        }


        Area2D? clickArea = GetNodeOrNull<Area2D>("Area2D");
        if (clickArea != null)
            clickArea.Connect("input_event", new Callable(this, nameof(OnClicked)));
        else
            GD.PrintErr("Area2D não encontrada no NPC.");
    }

    private void OnClicked(Node viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.Pressed &&
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            StartDialog("npc_dialogo_1");
        }
    }

    private void StartDialog(string timelineName)
    {
        if (dialogic == null)
        {
            GD.PrintErr("Dialogic node não encontrado!");
            return;
        }

        string timelinePath = $"res://Chars/{timelineName}.dtl";
        Resource? timelineResource = GD.Load<Resource>(timelinePath);
        if (timelineResource == null)
        {
            GD.PrintErr($"Falha ao carregar timeline: {timelinePath}");
            return;
        }

        Resource? characterResource = GD.Load<Resource>("res://Chars/Npc.dch");
        Resource? playerResource = GD.Load<Resource>("res://Chars/Player.dch");
        if (characterResource == null || playerResource == null)
        {
            GD.PrintErr("Falha ao carregar personagem .dch");
            return;
        }

        Node2D? marker = GetNodeOrNull<Node2D>("Marker2D");
        Node2D? playerMarker = GetNode<Node2D>("/root/World/Player/Marker2D");
        if (marker == null || playerMarker == null)
        {
            GD.PrintErr("Marker2D não encontrado.");
            return;
        }


        Variant layoutVariant = dialogic.Call("start", timelineResource);
        CanvasLayer? layout = layoutVariant.As<CanvasLayer>();
        if (layout == null)
        {
            GD.PrintErr("Layout retornado não é um CanvasLayer válido.");
            return;
        }
        layout.Call("register_character", characterResource, marker);
        layout.Call("register_character", playerResource, playerMarker);

        if (!isConnected)
        {
            dialogic.Connect("timeline_ended", new Callable(this, nameof(OnDialogFinished)));
            isConnected = true;
        }
    }

    private void OnDialogFinished()
    {
        if (dialogic != null && isConnected)
        {
            dialogic.Disconnect("timeline_ended", new Callable(this, nameof(OnDialogFinished)));
            isConnected = false;
        }
    }
}
