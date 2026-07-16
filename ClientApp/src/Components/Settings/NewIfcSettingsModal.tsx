import { useState } from 'react';
import {observer} from "mobx-react-lite";
import {DefaultButton, Dialog, DialogFooter, DialogType, PrimaryButton, TextField} from "@fluentui/react";
import {Loading} from "../Loading";
import {ApiCalls} from "../../Utilities/ApiCalls";
import {IfcSettingsSet} from "../../Utilities/DataTypes/IfcSettingsSet";
import {appState} from "../../App";

interface INewIfcSettingsModal{
    show: boolean;
    setShow: Function;
}

export const NewIfcSettingsModal = observer(({show, setShow}: INewIfcSettingsModal)=>{
    const [name, setName] = useState("");
    const [loading, setLoading] = useState(false);

    function toggleHideDialog(){
        setShow(false);
        setName("");
        setTimeout(()=>setLoading(false), 500);
    }

    function createSetName(){
        setLoading(true);
        ApiCalls.postIfcSettings({ifcSettings: new IfcSettingsSet({name, isDefault: !appState.ifcSettings.length})})
            .then((newIfcSettings)=>{
                appState.addIfcSettingsSet(new IfcSettingsSet(newIfcSettings));
                toggleHideDialog();
            })
    }

    return (
        <Dialog
            hidden={!show}
            onDismiss={toggleHideDialog}
            dialogContentProps={{
                type: DialogType.normal,
                title: "Create IFC Settings Set Name",
                subText: "",
            }}
        >
            {loading ? <Loading/> :
                <form onSubmit={createSetName}>
                    <TextField value={name} onChange={(e, value)=>setName(value || "")} description={"IFC Settings Set Name"}/>
                    <DialogFooter>
                        <PrimaryButton disabled={!name} type={"submit"} text="Create Set Name" />
                        <DefaultButton onClick={toggleHideDialog} text="Cancel" />
                    </DialogFooter>
                </form>
            }
        </Dialog>
    )
})