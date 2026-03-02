using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack", menuName = "Little Hero Journey/Combat/Attack")]
public class AttackSO : ScriptableObject
{
    public AnimatorOverrideController animatorOverrideController;
    public float damage;
}