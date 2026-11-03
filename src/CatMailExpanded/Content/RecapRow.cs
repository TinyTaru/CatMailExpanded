using System;
using CatMailExpanded.Core;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// An extra line on the end-of-shift recap screen for the gift wrapping room: a copy of the home delivery line
    /// placed right under it. The game reveals its lines one after another; ours appears together with the line
    /// after it (or the total), with the number of gifts handed over and their points.
    /// </summary>
    internal static class RecapRow
    {
        /// <summary>Set by GiftRoom when gifts were handed over this shift (the row stays hidden otherwise).</summary>
        public static bool Pending;
        static GameObject _row;
        static TMP_Text _amount, _score, _label;
        static GameObject _after;   // the row whose appearance reveals ours

        public static void Reset()
        {
            Pending = false;
            _row = null;
            _amount = _score = _label = null;
            _after = null;
        }

        static bool Ensure(RecapScreenInterface ui)
        {
            if (_row) return true;
            var amountTpl = ui.HomeDeliveryParcelAmountValueText;
            var scoreTpl = ui.HomeDeliveryScoreValueText;
            if (!amountTpl) return false;
            var tplRow = amountTpl.transform.parent;
            // some layouts nest the value texts a level deeper than the row
            if (tplRow && tplRow.parent != ui.ScoreEntriesParent && tplRow.parent && tplRow.parent.parent == ui.ScoreEntriesParent) tplRow = tplRow.parent;
            if (!tplRow) return false;
            _row = UnityEngine.Object.Instantiate(tplRow.gameObject, tplRow.parent);
            _row.name = "CME_RecapRow_Gifts";
            _row.transform.SetSiblingIndex(tplRow.GetSiblingIndex() + 1);
            _row.SetActive(false);
            // the copied texts: the two value texts sit at the same paths as the template's
            _amount = FindSame(tplRow, amountTpl.transform, _row.transform);
            _score = scoreTpl ? FindSame(tplRow, scoreTpl.transform, _row.transform) : null;
            foreach (var t in _row.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == _amount || t == _score) continue;
                // the label: drop its localization so ours sticks
                foreach (var loc in t.GetComponents<Il2CppI2.Loc.Localize>()) UnityEngine.Object.Destroy(loc);
                _label ??= t;
            }
            // the line that comes after the home delivery one in the game's sequence
            var parent = tplRow.parent;
            int next = tplRow.GetSiblingIndex() + 2;
            _after = next < parent.childCount ? parent.GetChild(next).gameObject : null;
            return true;
        }

        static TMP_Text FindSame(Transform tplRoot, Transform target, Transform copyRoot)
        {
            var path = "";
            var t = target;
            while (t && t != tplRoot) { path = path.Length == 0 ? t.name : t.name + "/" + path; t = t.parent; }
            var found = path.Length == 0 ? copyRoot : copyRoot.Find(path);
            return found ? found.GetComponent<TMP_Text>() : null;
        }

        /// <summary>Polled every frame while in the post office.</summary>
        public static void Update()
        {
            if (!Pending && !_row) return;
            RecapScreenInterface ui;
            try { ui = InterfaceManager.Instance?.RecapScreenInterface; }
            catch { return; }
            if (ui == null || !ui.gameObject.activeInHierarchy)
            {
                if (_row && _row.activeSelf) _row.SetActive(false);
                return;
            }
            if (!Pending) return;
            if (!Ensure(ui)) return;
            bool show = _after == null || _after.activeSelf || (ui.TotalScoreValueText && ui.TotalScoreValueText.gameObject.activeInHierarchy && !_after);
            if (show && !_row.activeSelf)
            {
                _row.SetActive(true);
                if (_label) _label.text = Localization.Text("CME/Recap_Gifts");
                if (_amount) _amount.text = GiftRoom.LastCount.ToString();
                if (_score) _score.text = (GiftRoom.LastScore >= 0 ? "+" : "") + GiftRoom.LastScore;
            }
        }

        /// <summary>A new shift: the recap is gone, and the next one only shows the row if gifts are handed over.</summary>
        public static void OnShiftStart()
        {
            Pending = false;
            if (_row) _row.SetActive(false);
        }
    }
}
