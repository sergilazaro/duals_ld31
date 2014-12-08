using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TweenManager : MonoBehaviour
{
	public enum EasingFunction
	{
		Linear,
		EaseOutBounce,
		Hermite,
		Exp,
		InvExp
	}

	private class Tween
	{
		public TimeSpan duration;
		public Action<float> onUpdate;
		public Action onFinish;

		public float startValue = 0;
		public float endValue = 1;

		public TimeSpan elapsed = TimeSpan.Zero;
		public EasingFunction easingFunction = EasingFunction.Linear;
	}

	public static TweenManager instance;

	HashSet<Tween> tweens = new HashSet<Tween>();

	List<Tween> tweensToAdd = new List<Tween>();

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		List<Tween> tweensToDelete = new List<Tween>();

		foreach (Tween tween in tweens)
		{
			tween.elapsed += TimeSpan.FromSeconds(Time.deltaTime);

			float normalized = Mathf.Clamp01((float)(tween.elapsed.TotalSeconds / tween.duration.TotalSeconds));
			float eased = EaseValue(normalized, tween.easingFunction);
			float scaled = tween.startValue + (tween.endValue - tween.startValue) * eased;

			tween.onUpdate.Invoke(scaled);

			if (normalized == 1.0f)
			{
				if (tween.onFinish != null)
				{
					tween.onFinish.Invoke();
				}

				tweensToDelete.Add(tween);
			}
		}

		foreach (Tween tween in tweensToDelete)
		{
			tweens.Remove(tween);
		}

		foreach (Tween tween in tweensToAdd)
		{
			tweens.Add(tween);
		}
		tweensToAdd.Clear();
	}

	public void AddTween(TimeSpan duration, Action<float> onUpdate, float startValue = 0, float endValue = 1, EasingFunction easingFunction = EasingFunction.Linear, Action onFinish = null)
	{
		tweensToAdd.Add(new Tween() { duration = duration, onUpdate = onUpdate, startValue = startValue, endValue = endValue, easingFunction = easingFunction, onFinish = onFinish });

		onUpdate(startValue);
	}

	public void ExecuteDelayedAction(TimeSpan delay, Action action)
	{
		ExecuteDelayedAction((float) delay.TotalSeconds, action);
	}

	public void ExecuteDelayedAction(float delay, Action action)
	{
		StartCoroutine(ExecuteDelayedAction_Coroutine(delay, action));
	}

	private IEnumerator ExecuteDelayedAction_Coroutine(float delay, Action action)
	{
		yield return new WaitForSeconds(delay);

		action.Invoke();
	}

	float EaseValue(float t, EasingFunction func)
	{
		switch (func)
		{
			case EasingFunction.Linear:
				return t;

			case EasingFunction.EaseOutBounce:
				return EaseOutBounce(t);

			case EasingFunction.Exp:
				return Exp(t);

			case EasingFunction.InvExp:
				return InvExp(t);

			case EasingFunction.Hermite:
				return Hermite(t);

			default:
				throw new NotImplementedException();
		}
	}

	public static float EaseOutBounce(float t)
	{
		float l1 = (1.0f / 2.75f);
		float l2 = (2f / 2.75f);
		float l3 = (2.5f / 2.75f);

		float m12 = (1.5f / 2.75f);
		float m23 = (2.25f / 2.75f);
		float m34 = (2.625f / 2.75f);

		float factor = 7.5625f;

		if (t < l1)
		{
			// t = 0..l1
			return (factor * t * t);
		}
		else if (t < l2)
		{
			t -= m12;

			return (factor * t * t + .75f);
		}
		else if (t < l3)
		{
			t -= m23;

			return (factor * t * t + .9375f);
		}
		else
		{
			t -= m34;
			return (factor * t * t + .984375f);
		}
	}

	public static float Hermite(float t)
	{
		return 3 * t * t - 2 * t * t * t;
	}

	public static float Exp(float t, float b = 0.1f)
	{
		return (Mathf.Pow(b, -t) - 1) / ((1.0f / b) - 1);
	}

	public static float InvExp(float t, float b = 0.1f)
	{
		return 1 - Exp(1 - t, b);
	}
}
