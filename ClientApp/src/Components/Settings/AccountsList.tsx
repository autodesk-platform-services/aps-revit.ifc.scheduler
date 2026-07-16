import React from 'react';
import {observer} from "mobx-react-lite";
import {DetailsList, SelectionMode, Toggle} from "@fluentui/react";
import {appState} from "../../App";
import {useTranslation} from "react-i18next";
import {Account} from "../../Utilities/DataTypes/Account";

export const AccountsList = observer(()=>{
    const { t } = useTranslation();
    return (
        <DetailsList
            selectionMode={SelectionMode.none}
            columns={[
                {
                    key: "name",
                    name: t("BIM 360 Accounts"),
                    fieldName: "name",
                    minWidth: 70
                },
                {
                    key: "enabled",
                    name: t("Include in Project List"),
                    fieldName: "enabled",
                    minWidth: 150,
                    onRender: (item: Account)=><AccountToggle account={item}/>
                }
            ]}
            items={appState.accounts}
        />
    )
})

const AccountToggle = observer(({account}: {account: Account})=>{
    return(<div style={{display: 'flex', alignItems: 'center', justifyContent: 'center'}}>
        <Toggle checked={account.enabled} onChange={()=>account.toggleEnabled()}/>
    </div>)
})