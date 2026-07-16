import React, {useState} from 'react';
import {observer} from "mobx-react-lite";
import {DefaultButton, Dialog, DialogFooter, DialogType, PrimaryButton} from "@fluentui/react";
import {Schedule} from "../Utilities/DataTypes/Schedule";
import { useTranslation } from 'react-i18next';
import Creatable from "react-select/creatable";
import {appState} from "../App";
import {Project} from "../Utilities/DataTypes/Project";
import {ApiCalls} from "../Utilities/ApiCalls";
import {Loading} from "./Loading";

export const ConvertToIfcNowModal = observer(({project, schedule, show, setShow}
                                                  : {project: Project, schedule?: Schedule, show: boolean, setShow: Function})=>{

    const {t} = useTranslation();
    const [ifcSettingsName, setIfcSettingsName] = useState<string | undefined>()
    const [loading, setLoading] = useState(false);

    function convertSelected(){
        //If Schedule prop exists, use those selected files. If not, use the project selected files



        setLoading(true);
        const base = schedule ? schedule : project;

        const files = base.checked
            .filter(val=>val.indexOf("_placeholderChild") === -1)
            .filter(val=>val.indexOf("urn:adsk.wipprod:fs.file") > -1)
            .map(val=>project.filesHash[val]!.toInterface());

        const folderIds = base.checked
            .filter(val=>val.indexOf("_placeholderChild") === -1)
            .filter(val=>val.indexOf("urn:adsk.wipprod:fs.folder") > -1);

        ApiCalls
            .PostConversion({project, files: files, folderUrns: folderIds, ifcSettingsName: ifcSettingsName || ""})
            .finally(()=>{
                setShow(false);
                setLoading(false);
            })
    }

    const dialogContentProps = {
        type: DialogType.normal,
        title: t('Convert to IFC Now'),
        closeButtonAriaLabel: t('Close')
    }

    return (
        <React.Fragment>
            <Dialog
                hidden={!show}
                onDismiss={()=>setShow(false)}
                dialogContentProps={dialogContentProps}
            >
                {loading ? <Loading/>
                    :
                    <form onSubmit={convertSelected}>
                        <div style={{marginBottom: '10px'}}>
                            <label>{t('IFC Settings Set Name in Revit')}</label>
                        </div>
                        <div>
                            <Creatable
                                name={"ifcSettingsModal"}
                                //@ts-ignore
                                menuPortalTarget={document.querySelector("#fronter")}
                                options={appState.ifcSettings.map(val=>val.listValue)}
                                value={ifcSettingsName ? {label: ifcSettingsName, value: ifcSettingsName} : undefined}
                                onCreateOption={(value)=>setIfcSettingsName(value)}
                                onChange={(option)=>{
                                    //@ts-ignore -- value is a valid option for OptionType
                                    setIfcSettingsName(option?.value || "");
                                }}
                            />
                        </div>

                        <DialogFooter>
                            <PrimaryButton style={{float: 'left'}} type={'submit'} text={t("Convert to IFC Now")} />
                            <DefaultButton onClick={()=>setShow(false)} text={t("Cancel")} />
                        </DialogFooter>
                    </form>}
            </Dialog>
            <div id={'fronter'} style={{zIndex: 10000000}}/>
        </React.Fragment>
    )
})