import { Fragment, useState } from 'react';
import {observer} from "mobx-react-lite";
import {DefaultButton, DetailsList, SelectionMode} from "@fluentui/react";
import {appState} from "../../App";
import {useTranslation} from "react-i18next";
import {DeleteIfcSettingsModal} from "./DeleteIfcSettingsModal";
import {IfcSettingsSet} from "../../Utilities/DataTypes/IfcSettingsSet";

export const IfcSettingsList = observer(()=>{
    const { t } = useTranslation();
    const [show, setShow] = useState(false);
    const [selectedIfcSettings, setSelectedIfcSettings] = useState<IfcSettingsSet | undefined>();

    function selectIfcSettings(ifcSettings: IfcSettingsSet){
        setShow(true);
        setSelectedIfcSettings(ifcSettings);
    }

    return (
        <Fragment>
            <DetailsList
                selectionMode={SelectionMode.none}
                columns={[
                    {
                        key: "name",
                        name: t("IFC Settings Set Names"),
                        fieldName: "name",
                        minWidth: 70,
                        onRender: (item: any)=>item.value === 'toggleAll' ? <b>{item.name}</b> : item.name
                    },
                    {
                        key: "id",
                        name: "",
                        fieldName: "id",
                        minWidth: 120,
                        onRender: (item: any)=>(<DefaultButton onClick={()=>selectIfcSettings(item)} text={t('Delete')}/>)
                    }
                    ]}
                items={appState.ifcSettings}
            />
            <DeleteIfcSettingsModal show={show} setShow={setShow} ifcSettings={selectedIfcSettings}/>
        </Fragment>
    );
})