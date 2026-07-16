import { useState } from 'react';
import {observer} from "mobx-react-lite";
import {DefaultButton, Dialog, DialogFooter, DialogType, PrimaryButton, Text} from "@fluentui/react";
import {Loading} from "../Loading";
import {ApiCalls} from "../../Utilities/ApiCalls";
import {IfcSettingsSet} from "../../Utilities/DataTypes/IfcSettingsSet";
import {appState} from "../../App";
import { useTranslation } from 'react-i18next';

interface IDeleteIfcSettingsModal{
    show: boolean;
    setShow: Function;
    ifcSettings?: IfcSettingsSet;
}

export const DeleteIfcSettingsModal = observer(({show, setShow, ifcSettings}: IDeleteIfcSettingsModal)=>{
    const {t} = useTranslation();
    const [loading, setLoading] = useState(false);

    function toggleHideDialog(){
        setShow(false);
        setTimeout(()=>setLoading(false), 500);
    }

    function createSetName(){
        if(ifcSettings){
            setLoading(true);
            ApiCalls.deleteIfcSettings({ifcSettings})
                .then(()=>{
                    appState.removeIfcSettingsSet(ifcSettings);
                    toggleHideDialog();
                })
        }
    }

    return (
        <Dialog
            hidden={!show}
            onDismiss={toggleHideDialog}
            dialogContentProps={{
                type: DialogType.normal,
                title: t("Delete Settings Set Name"),
            }}
        >
            {loading ? <Loading/> :
                <form onSubmit={createSetName}>
                    <Text>
                        {t("Are you sure you want to delete")}
                        {ifcSettings?.name}"?
                        {t("This will remove it from the preset list, but will not remove it from any existing or scheduled conversion jobs.")}
                    </Text>
                    <DialogFooter>
                        <PrimaryButton disabled={!ifcSettings} type={"submit"} text={t("Delete Set Name")} />
                        <DefaultButton onClick={toggleHideDialog} text={t("Cancel")} />
                    </DialogFooter>
                </form>
            }
        </Dialog>
    )
})