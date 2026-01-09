using UnityEngine;
using System.Collections.Generic;

namespace VRDrawing.Data
{
    public class DrawingHistoryManager
    {
        private Stack<DrawingData> undoStack = new Stack<DrawingData>();
        private Stack<DrawingData> redoStack = new Stack<DrawingData>();
        private int maxHistorySize = 50;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        public int UndoCount => undoStack.Count;
        public int RedoCount => redoStack.Count;

        public DrawingHistoryManager(int maxHistorySize = 50)
        {
            this.maxHistorySize = maxHistorySize;
        }

        public void RecordState(DrawingData currentState)
        {
            if (currentState == null) return;

            undoStack.Push(currentState.Clone());

            if (undoStack.Count > maxHistorySize)
            {
                var tempStack = new Stack<DrawingData>();
                for (int i = 0; i < maxHistorySize; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack = tempStack;
            }

            redoStack.Clear();
        }

        public DrawingData Undo(DrawingData currentState)
        {
            if (!CanUndo) return currentState;

            redoStack.Push(currentState.Clone());
            return undoStack.Pop();
        }

        public DrawingData Redo(DrawingData currentState)
        {
            if (!CanRedo) return currentState;

            undoStack.Push(currentState.Clone());
            return redoStack.Pop();
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
