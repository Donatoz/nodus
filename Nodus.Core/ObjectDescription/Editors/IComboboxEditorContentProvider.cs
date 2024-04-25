using System.Collections.Generic;

namespace Nodus.Core.ObjectDescription;

public interface IComboboxEditorContentProvider
{
    string[] GetOptions();
}