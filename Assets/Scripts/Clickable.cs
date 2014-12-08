using UnityEngine;
using System.Collections;

public class Clickable : MonoBehaviour
{
	public int tileX;
	public int tileY;

	public Board board;

	void OnMouseDown()
	{
		if (board != null)
		{
			board.ClickedOnTile(tileX, tileY);
		}
		else
		{
			Clickable parentClickable = transform.parent.GetComponent<Clickable>();

			if (parentClickable != null)
			{
				parentClickable.board.ClickedOnTile(parentClickable.tileX, parentClickable.tileY);
			}
		}
	}
}
