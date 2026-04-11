import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { categoriesApi, servicesApi } from '../../api'
import { useToast } from '../../hooks/useUtils'
import TopBar from '../../components/layout/TopBar'
import Button from '../../components/ui/Button'
import Input from '../../components/ui/Input'
import styles from './CreateService.module.css'

export default function CreateServicePage() {
  const navigate = useNavigate()
  const toast = useToast()
  
  const [categories, setCategories] = useState([])
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState({
    categoryId: '',
    title: '',
    description: '',
    basePrice: '',
    hourlyRate: '',
    isRemote: false,
    areaName: '',
    newCategoryName: ''
  })

  useEffect(() => {
    categoriesApi.getAll().then(res => setCategories(res.data)).catch(console.error)
  }, [])

  const set = k => e => {
    const val = e.target.type === 'checkbox' ? e.target.checked : e.target.value
    setForm(f => ({ ...f, [k]: val }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.categoryId) { toast('Please select a category', 'error'); return }

    setLoading(true)
    try {
      let finalCategoryId = parseInt(form.categoryId, 10);
      if (form.categoryId === 'NEW') {
         if (!form.newCategoryName) { toast('New category name required', 'error'); setLoading(false); return }
         const catRes = await categoriesApi.create({ name: form.newCategoryName, description: "User generated category" });
         finalCategoryId = catRes.data.id;
      }

      await servicesApi.create({
        ...form,
        categoryId: finalCategoryId,
        basePrice: parseFloat(form.basePrice || 0),
        hourlyRate: parseFloat(form.hourlyRate || 0)
      })
      toast('Service created successfully!', 'success')
      navigate('/profile')
    } catch (err) {
      toast(err?.response?.data?.message || err?.response?.data || 'Failed to create service', 'error')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.page}>
      <TopBar title="Host Service" />
      <div className={styles.content}>
        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.inputGroup}>
            <label className={styles.label}>Category</label>
            <select className={styles.select} value={form.categoryId} onChange={set('categoryId')} required>
              <option value="">Select a Category...</option>
              <option value="NEW">➕ Create New Category</option>
              {categories.map(c => (
                <optgroup key={c.id} label={c.name}>
                  <option value={c.id}>{c.name} - General</option>
                  {(c.subCategories || []).map(sub => (
                    <option key={sub.id} value={sub.id}>{sub.name}</option>
                  ))}
                </optgroup>
              ))}
            </select>
          </div>

          {form.categoryId === 'NEW' && (
            <div className={styles.inputGroup} style={{ backgroundColor: 'var(--surface-color)', padding: 12, borderRadius: 8 }}>
              <Input label="New Category Name" placeholder="e.g. Drone Piloting" value={form.newCategoryName} onChange={set('newCategoryName')} required />
            </div>
          )}

          <Input label="Service Title" placeholder="e.g. Expert Plumber in Ikeja" value={form.title} onChange={set('title')} required minLength={5} />
          
          <div className={styles.inputGroup}>
            <label className={styles.label}>Service Description</label>
            <textarea className={styles.textarea} placeholder="Describe what you offer in detail..." value={form.description} onChange={set('description')} required minLength={10} rows={4} />
          </div>

          <div style={{ display: 'flex', gap: 12 }}>
            <div style={{ flex: 1 }}>
               <Input label="Base Price (₦)" type="number" placeholder="Fixed Rate" value={form.basePrice} onChange={set('basePrice')} min={100} required />
            </div>
            <div style={{ flex: 1 }}>
               <Input label="Hourly Rate (₦)" type="number" placeholder="Rate/hr" value={form.hourlyRate} onChange={set('hourlyRate')} min={0} />
            </div>
          </div>

          <div className={styles.toggleRow}>
            <span>Is this a purely remote service?</span>
            <input type="checkbox" checked={form.isRemote} onChange={set('isRemote')} className={styles.toggle} />
          </div>

          {!form.isRemote && (
            <Input label="Service Area" placeholder="e.g. Lekki Phase 1, Lagos" value={form.areaName} onChange={set('areaName')} required={!form.isRemote} />
          )}

          <Button type="submit" block size="lg" loading={loading} style={{ marginTop: 16 }}>Publish Service</Button>
        </form>
      </div>
    </div>
  )
}
