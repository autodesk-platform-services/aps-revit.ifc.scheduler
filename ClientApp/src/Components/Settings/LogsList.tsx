import {observer} from "mobx-react-lite";
import {DefaultButton, DetailsList, SelectionMode} from "@fluentui/react";
import {appState} from "../../App";
import {useTranslation} from "react-i18next";
import {Log} from "../../Utilities/DataTypes/Log";
import {ApiCalls} from "../../Utilities/ApiCalls";
import FileSaver from "file-saver";

export const LogsList = observer(()=>{
    const { t } = useTranslation();

    function DownloadLogs(log: Log){
        ApiCalls.getLogFile({log})
            .then(file=>{

                let bytes = new Uint8Array(file.length);

                for (let i = 0; i < bytes.length; i++) {
                    bytes[i] = file.charCodeAt(i);
                }

                const blob = new Blob([bytes], {type: "text/plain"})

                FileSaver.saveAs(blob, log.name);
            })
    }

    return (
        <DetailsList
            selectionMode={SelectionMode.none}
            columns={[
                {
                    key: "name",
                    name: t("Log Files"),
                    fieldName: "name",
                    minWidth: 70,
                    onRender: (item: any)=>item.value === 'toggleAll' ? <b>{item.name}</b> : item.name
                },
                {
                    key: "id",
                    name: "",
                    fieldName: "id",
                    minWidth: 120,
                    onRender: (item: any)=>(<DefaultButton onClick={()=>DownloadLogs(item)} text={t("Download")}/>)
                }
            ]}
            items={appState.logs}
        />
    )
})