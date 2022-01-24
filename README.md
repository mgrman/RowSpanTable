# RowSpanTable
 
RowSpanTable project implements a Table script that supports cells with `colSpan` and `rowSpan` attributes.
This is intended for now as a **proof of concept**.

![Sample image](Docs\sample.png)
- Red cell is first cell in first row with `rowSpan` set to 2 and `colSpan` set to 1 (the darker section highlight the first row)
- Yellow cell is second cell in first row with `rowSpan` set to 1 and `colSpan` set to 2
- Green cell is first cell in second row  with `rowSpan` set to 1 and `colSpan` set to 1
- Blue cell is second cell in second row  with `rowSpan` set to 1 and `colSpan` set to 1

In HTML it would be:
```
<table>
    <tr>
        <td rowSpan="2">
        </td>
        <td colSpan="2">
        </td>
    </tr>
    <tr>
        <td>
        </td>
        <td>
        </td>
    </tr>
</table>
```

## Supported features

- Cells with defined `colSpan` and `rowSpan`
- Row and Column sizes are computed by auto-sizing to preferred width/height.

It does not have a lot of features but it can act as a basis for extending it based on your requirements.