import { useEffect, useState } from "react";
import {observer} from "mobx-react-lite";
import {IColumn, SearchBox} from "@fluentui/react";
import {SortedTable} from "./SortedTable";

interface ISearchableAndSortableTable {
    title?: string;
    placeholder?: string;
    items: any[];
    onItemInvoked: any;
    columns: IColumn[];
    onRenderRow?: any,
    sortedColumnKey?: string;
    sortedColumnKeyDirection?: boolean;
    selectedRowId?: string;
}

export const SearchableAndSortableTable = observer(({
                                                        title,
                                                        placeholder,
                                                        items,
                                                        onItemInvoked,
                                                        columns,
                                                        onRenderRow,
                                                        sortedColumnKey,
                                                        sortedColumnKeyDirection,
                                                        selectedRowId
                                                    }: ISearchableAndSortableTable)=>{

    const [search, setSearch] = useState("");
    const [searchedItems, setSearchedItems] = useState(items);

    useEffect(()=>{
        if(search){
            var lowerCaseTerm = search.toLowerCase();
            setSearchedItems(items.filter(val=>val.searchTerm.indexOf(lowerCaseTerm) > -1))
        } else {
            setSearchedItems(items);
        }
    }, [search, items])

    return (
        <div style={{display: "flex", flexDirection: 'column', padding: "0 10px"}}>
            <h3>{title}</h3>
            <SearchBox
                placeholder={placeholder}
                inputMode={"search"}
                value={search}
                onChange={(e, value)=>setSearch(value || "")}
            />
            <div style={{overflowY: "auto", flex: 1}}>
                <SortedTable
                    items={searchedItems}
                    onItemInvoked={onItemInvoked}
                    columns={columns}
                    onRenderRow={onRenderRow}
                    sortedColumnKey={sortedColumnKey}
                    sortedColumnKeyDirection={sortedColumnKeyDirection}
                    selectedRowId={selectedRowId}
                />
            </div>
        </div>
    )
})