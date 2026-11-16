using PrimeTween;
using UnityEngine;

public interface IPanelTransition
{
    Sequence Transition(GameObject from, GameObject to);
}
