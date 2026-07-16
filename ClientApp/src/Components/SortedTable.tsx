import { useEffect, useState } from 'react';
import {DetailsList, DetailsRow, IColumn, SelectionMode} from "@fluentui/react";
import {observer} from "mobx-react-lite";

interface ISortedTable {
  items: any[];
  onItemInvoked: any;
  columns: IColumn[];
  onRenderRow?: any,
  sortedColumnKey?: string;
  sortedColumnKeyDirection?: boolean;
  selectedRowId?: string;
}

export const SortedTable = observer(({items, onItemInvoked, columns, onRenderRow, sortedColumnKey, sortedColumnKeyDirection, selectedRowId}: ISortedTable)=>{
  const [currentColumns, setCurrentColumns] = useState<IColumn[]>(columns.slice());
  const [lastColumn, setLastColumn] = useState<IColumn>();
  const [sortedItems, setSortedItems] = useState<any[]>(items.slice());

  useEffect(()=>{
    if(sortedColumnKey){
      const column = columns.find(val=>val.key === sortedColumnKey);
      if(column){
        setLastColumn(column);

        if(sortedColumnKeyDirection){
          onColumnClick(null, column, true);
        }
      }
    }
    //eslint-disable-next-line
  }, [])

  useEffect(()=>{
      if(lastColumn){
        onColumnClick(null, lastColumn, true);
      } else {
        setSortedItems(items);
      }
      //eslint-disable-next-line
  }, [items]);

  const onColumnClick = (ev: any, column: IColumn, keepSort?: boolean)=>{
    const newColumns: IColumn[] = columns.slice();
    const currColumn: IColumn = newColumns.filter(currCol => column.key === currCol.key)[0];

    newColumns.forEach((newCol: IColumn) => {
      if (newCol === currColumn) {
        currColumn.isSortedDescending = keepSort ? !!column.isSortedDescending : !currColumn.isSortedDescending;
        currColumn.isSorted = true;
        setLastColumn(currColumn);
      } else {
        newCol.isSorted = false;
        newCol.isSortedDescending = false;
      }
    });

    const sorted = items.slice().sort((a,b)=>{
      const second: any = currColumn.isSortedDescending ? a[currColumn.fieldName || currColumn.key] : b[currColumn.fieldName || currColumn.key];
      const first: any = currColumn.isSortedDescending ? b[currColumn.fieldName || currColumn.key] : a[currColumn.fieldName || currColumn.key];

      if(!first){
        return 1;
      }
      if(!second){
        return -1;
      }

      if(currColumn.data === 'dayjs'){
        return first.isBefore(second) ? 1 : -1;
      }

      //@ts-ignore
      return first.localeCompare(second, 'en', {numeric: true});
    });

    setCurrentColumns(newColumns);
    setSortedItems(sorted);
  }

  const filtered = currentColumns.map(val=>{
    val.onColumnClick = onColumnClick;
    return val;
  });

  return (
      <DetailsList
          items={sortedItems}
          selectionMode={SelectionMode.none}
          onItemInvoked={onItemInvoked}
          onRenderRow={onRenderRow ? onRenderRow : (props)=>{
            //@ts-ignore
            return <DetailsRow {...props} onClick={()=>onItemInvoked(props?.item)} styles={{cell: {cursor: 'pointer', backgroundColor: selectedRowId === props?.item.id ? '#cbd3fc': 'white'}}}/>}}
          columns={filtered}
      />
  );
});